module CentralAgent

open System.Net

type State =
    { UsersDb : Users.UsersData
    }

type Request =
    | UpdateUserDb
    | SendOrder of string
    | GetPlayerList of AsyncReplyChannel<RemoteConsole.PlayerData [] option>
    | TryLogin of string * WebSharper.Web.IContext * AsyncReplyChannel<string option>

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
                do! client.MessagePlayer(clientId, sprintf "Your pin code: %s" usersDb.Value.Passwords.[user])
        return usersDb.Value
    }

let agent = MailboxProcessor.Start(fun inbox ->
    let rec loop state = async {
        let! msg = inbox.Receive()
        match msg with
        | UpdateUserDb ->
            use! client = connect
            let! users = updateUserDb client state.UsersDb
            return! loop { state with UsersDb = users }
        | SendOrder order ->
            use! client = connect
            let! _ = client.ServerInput(order)
            return! loop state
        | GetPlayerList reply ->
            use! client = connect
            let! players = client.GetPlayerList()
            reply.Reply players
            return! loop state
        | TryLogin(password, ctx, reply) ->
            use! client = connect
            let! users = updateUserDb client state.UsersDb
            let! result =
                match users.TryGetPasswordOwner(password) with
                | Some(Users.Named name) ->
                    async {
                        do! ctx.UserSession.LoginUser(name)
                        return Some name
                    }
                | None ->
                    async {
                        return None
                    }
            reply.Reply result
            return! loop { state with UsersDb = users }
        return! loop state
    }
    let state =
        { UsersDb = Users.UsersData.Default
        }
    loop state
)