module Users

type Coalition = Axis | Allies

type Coalition with
    member this.AsString() =
        match this with
        | Axis -> "Axis"
        | Allies -> "Allies"

type Unit = Platoon of string

type User = Named of string

let newCoallitionPassword =
    let random = System.Random()
    let letters = "ABCDEFGHJKLMNPRSTUVWXYZ"
    fun () ->
        seq {
            for i in 1..3 do
                yield letters.[random.Next(0, letters.Length - 1)]
        }
        |> Seq.map (fun c -> c.ToString())
        |> String.concat ""

let newUserPassword =
    let random = System.Random()
    let digits = "123456789"
    fun () ->
        seq {
            for i in 1..4 do
                yield digits.[random.Next(0, digits.Length - 1)]
        }
        |> Seq.map (fun c -> c.ToString())
        |> String.concat ""

type UsersData =
    { UserNames : User list
      ClientIds : Map<User, int>
      Passwords : Map<User, string>
      ControlledBy : Map<Unit, User>
      CoalitionPasswords : Map<Coalition, string>
      Coalitions : Map<User, Coalition>
    }
with
    static member Default =
        {
            UserNames = []
            ClientIds = Map.empty
            Passwords = Map.empty
            CoalitionPasswords = Map.empty
            ControlledBy = Map.empty
            Coalitions = Map.empty
        }

    member this.AddUser(user) =
        match List.tryFind ((=) user) this.UserNames with
        | Some _ ->
            this
        | None ->
            let rec newUniquePassword() =
                let candidate = newUserPassword()
                if Map.exists (fun _ pwd -> pwd = candidate) this.Passwords then
                    newUniquePassword()
                else
                    candidate
            let password = newUniquePassword()
            { this with
                UserNames = user :: this.UserNames
                Passwords = Map.add user password this.Passwords
            }

    member this.SetClientId(user, clientId) =
        { this with
            ClientIds = this.ClientIds |> Map.add user clientId
        }

    member this.RemoveUser(user) =
        { this with
            UserNames = this.UserNames |> List.filter ((<>) user)
            ClientIds = this.ClientIds |> Map.remove user
            Passwords = this.Passwords |> Map.remove user
            ControlledBy = this.ControlledBy |> Map.filter (fun _ owner -> owner <> user)
        }

    member this.TryGetPasswordOwner(password) =
        this.Passwords
        |> Map.tryPick(fun user pwd ->
            if password = pwd then
                Some user
            else
                None)

    member this.TryGetCoalition(password) =
        this.CoalitionPasswords
        |> Map.tryPick(fun coalition pwd ->
            if password = pwd then
                Some coalition
            else
                None)

    member this.Login(user, coalition) =
        // If the user switched coalition, clear its controlled units.
        let controlled2 =
            match Map.tryFind user this.Coalitions with
            | Some(other) when other <> coalition ->
                this.ControlledBy
                |> Map.filter(fun platoon owner -> owner <> user)
            | Some(_)
            | None ->
                this.ControlledBy
        let coalition2 =
            this.Coalitions
            |> Map.add user coalition
        { this with
            Coalitions = coalition2
            ControlledBy = controlled2 }
        
    member this.AddUsers(usernames) =
        let newUsers =
            usernames
            |> Seq.filter (fun username ->
                this.UserNames
                |> List.exists (fun (Named user) -> user = username)
                |> not
            )
            |> Set.ofSeq
        let next =
            usernames
            |> Seq.fold(fun (data : UsersData) username -> data.AddUser(Named username)) this
        next, newUsers

    member this.RemoveUsers(usernames) =        
        usernames
        |> Seq.fold(fun (data : UsersData) username -> data.RemoveUser(Named username)) this
