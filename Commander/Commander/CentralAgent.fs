module CentralAgent

open System.Net

type State =
    { UsersDb : Users.UsersData
    }

type Request =
    | UpdateUserDb
    | TryLogin of userPwd : string * coalitionPwd : string * AsyncReplyChannel<(Users.User * Users.Coalition) option>
    | FilterAvailable of platoons : Users.Unit list * AsyncReplyChannel<Users.Unit list>
    | TryGrab of user : Users.User * platoon : Users.Unit * AsyncReplyChannel<bool>
    | Release of user : Users.User * platoon : Users.Unit
    | UserControls of user : Users.User * platoon : Users.Unit * AsyncReplyChannel<bool>
    | GetUserPlatoons of user : Users.User * AsyncReplyChannel<Users.Unit list>
    | SendOrder of string
    | Logout of user : Users.User
    | GetPlayerList of AsyncReplyChannel<RemoteConsole.PlayerData [] option>

let connect =
    async {
        let config = Configuration.values
        let rconIP = IPAddress.Parse(config.RconAddress)
        let client = new RemoteConsole.Client(rconIP, config.RconPort, config.RconUsername, config.RconPassword)
        let! _ = client.Auth()
        return client
    }

let updateUserDb (client : RemoteConsole.Client) (usersDb : Users.UsersData) =
    async {
        let! players = client.GetPlayerList()
        let usernames =
            match players with
            | Some data ->
                data
                |> Seq.map(fun player -> player.Name, player.ClientId)
            | None ->
                Seq.empty
        let usersDb, newUsers = usersDb.AddUsers(usernames |> Seq.map fst)
        let currentUsers = usernames |> Seq.map fst |> Set.ofSeq
        let oldUsers =
            usersDb.UserNames
            |> List.filter (fun (Users.Named name) -> Set.contains name currentUsers |> not)
            |> List.map (fun (Users.Named name) -> name)
        let usersDb = usersDb.RemoveUsers oldUsers
        let usersDb = ref usersDb
        if not newUsers.IsEmpty then
            do! client.MessageTeam(2, sprintf "Team password: %s" usersDb.Value.CoalitionPasswords.[Users.Axis])
            do! client.MessageTeam(1, sprintf "Team password: %s" usersDb.Value.CoalitionPasswords.[Users.Allies])
        for username, clientId in usernames do
            let user = Users.Named username
            usersDb := usersDb.Value.SetClientId(user, clientId)
            if Set.contains username newUsers then
                if Configuration.values.WebListeningAddresses |> Array.isEmpty |> not then
                    do! client.MessagePlayer(clientId, sprintf "Ground commander at %s" Configuration.values.WebListeningAddresses.[0])
                do! client.MessagePlayer(clientId, sprintf "Your pin code: %s" usersDb.Value.Passwords.[user])
        return usersDb.Value
    }

let agent = MailboxProcessor.Start(fun inbox ->
    let rec loop client state = async {
        let! msg = inbox.Receive()
        match msg with
        | UpdateUserDb ->
            printfn "Updating user db..."
            let! users = updateUserDb client state.UsersDb
            printfn "Done."
            return! loop client { state with UsersDb = users }
        | TryLogin(userPwd, coalitionPwd, reply) ->
            printfn "Trying to login user with pin code %s..." (coalitionPwd + userPwd)
            let! result, usersDb2 =
                match state.UsersDb.TryGetPasswordOwner(userPwd), state.UsersDb.TryGetCoalition(coalitionPwd) with
                | Some(user), Some(coalition) ->
                    async {
                        printfn "Succeeded in coalition %A" coalition
                        return Some(user, coalition), state.UsersDb.Login(user, coalition)
                    }
                | _, None
                | None, _ ->
                    async {
                        printfn "Failed."
                        return None, state.UsersDb
                    }
            reply.Reply result
            return! loop client { state with UsersDb = usersDb2 }
        | FilterAvailable(platoons, reply) ->
            let available = state.UsersDb.FilterAvailable(platoons)
            reply.Reply available
            return! loop client state
        | TryGrab(user, platoon, reply) ->
            printfn "User %s tries to grab %s" (user.AsString()) (platoon.AsString())
            let ok, users2 =
                match state.UsersDb.ControlledBy |> Map.tryFind platoon with
                | None -> true, state.UsersDb.Grab(user, platoon)
                | Some _ -> false, state.UsersDb
            if ok then
                printfn "Succeeded"
            else
                printfn "Failed"
            reply.Reply ok
            return! loop client { state with UsersDb = users2 }
        | Release(user, platoon) ->
            printfn "User %s releases %s" (user.AsString()) (platoon.AsString())
            let users2 = state.UsersDb.Release(user, platoon)
            return! loop client { state with UsersDb = users2 }
        | UserControls(user, platoon, reply) ->
            reply.Reply <| state.UsersDb.UserControls(user, platoon)
            return! loop client state
        | GetUserPlatoons(user, reply) ->
            reply.Reply <| state.UsersDb.UnitsOf(user)
            return! loop client state
        | SendOrder order ->
            printfn "Sending an order..."
            let! _ = client.ServerInput(order)
            printfn "Done."
            return! loop client state
        | Logout user ->
            printfn "Logging out %s" (user.AsString())
            let users2 = state.UsersDb.RemoveUser(user)
            let! users3 = updateUserDb client users2
            return! loop client { state with UsersDb = users3 }
        | GetPlayerList reply ->
            printfn "Getting player list..."
            let! players = client.GetPlayerList()
            printfn "Done."
            reply.Reply players
            return! loop client state
    }
    async {
        let state =
            { UsersDb =
                { Users.UsersData.Default with
                    CoalitionPasswords =
                        Map.ofList [
                            (Users.Axis, Users.newCoallitionPassword())
                            (Users.Allies, Users.newCoallitionPassword())]
                }
            }
        use! client = connect
        return! loop client state
    }
)