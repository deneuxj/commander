﻿module CentralAgent

open System.Net

type State =
    { UsersDb : Users.UsersData
    }

type Request =
    | UpdateUserDb
    | SendOrder of string
    | GetPlayerList of AsyncReplyChannel<RemoteConsole.PlayerData [] option>
    | TryLogin of userPwd : string * coalitionPwd : string * AsyncReplyChannel<(string * string) option>

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
        for username, clientId in usernames do
            let user = Users.Named username
            usersDb := usersDb.Value.SetClientId(user, clientId)
            if Set.contains username newUsers then
                if Configuration.values.WebListeningAddresses |> Array.isEmpty |> not then
                    do! client.MessagePlayer(clientId, sprintf "Ground commander at %s" Configuration.values.WebListeningAddresses.[0])
                do! client.MessageTeam(2, sprintf "Team password: %s" usersDb.Value.CoalitionPasswords.[Users.Axis])
                do! client.MessageTeam(1, sprintf "Team password: %s" usersDb.Value.CoalitionPasswords.[Users.Allies])
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
        | SendOrder order ->
            printfn "Sending an order..."
            let! _ = client.ServerInput(order)
            printfn "Done."
            return! loop client state
        | GetPlayerList reply ->
            printfn "Getting player list..."
            let! players = client.GetPlayerList()
            printfn "Done."
            reply.Reply players
            return! loop client state
        | TryLogin(userPwd, coalitionPwd, reply) ->
            printfn "Trying to login user with pin code %s..." (coalitionPwd + userPwd)
            let! result, usersDb2 =
                match state.UsersDb.TryGetPasswordOwner(userPwd), state.UsersDb.TryGetCoalition(coalitionPwd) with
                | Some((Users.Named name) as usr), Some(coalition) ->
                    async {
                        printfn "Succeeded in coalition %A" coalition
                        return Some(name, coalition.AsString()), state.UsersDb.Login(usr, coalition)
                    }
                | _, None
                | None, _ ->
                    async {
                        printfn "Failed."
                        return None, state.UsersDb
                    }
            reply.Reply result
            return! loop client { state with UsersDb = usersDb2 }
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