module Users

type Coalition = Axis | Allies

type Unit = Platoon of string

type User = Named of string

let newPassword =
    let random = System.Random()
    let letters = "ABCDEFGHJKLMNPRSTUVWXYZ"
    let digits = "123456789"
    fun () ->
        seq {
            for i in 1..3 do
                yield letters.[random.Next(0, letters.Length - 1)]
            for i in 1..4 do
                yield digits.[random.Next(0, digits.Length - 1)]
        }
        |> Seq.map (fun c -> c.ToString())
        |> String.concat ""

type UsersData =
    { UserNames : User list
      ClientIds : Map<User, int>
      Passwords : Map<User, string>
      Coalitions : Map<User, Coalition>
      ControlledBy : Map<Unit, User>
    }
with
    member this.AddUser(user) =
        match List.tryFind ((=) user) this.UserNames with
        | Some _ ->
            this
        | None ->
            let rec newUniquePassword() =
                let candidate = newPassword()
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
            Coalitions = this.Coalitions |> Map.remove user
            ControlledBy = this.ControlledBy |> Map.filter (fun _ owner -> owner <> user)
        }

    member this.TryGetPasswordOwner(password) =
        this.Passwords
        |> Map.tryPick(fun user pwd ->
            if password = pwd then
                Some user
            else
                None
        )

type UsersData with
    static member Default =
        {
            UserNames = []
            ClientIds = Map.empty
            Passwords = Map.empty
            Coalitions = Map.empty
            ControlledBy = Map.empty
        }
        
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
