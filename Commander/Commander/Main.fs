module Commander
open System
open System.Net
open System.Text
open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.Sitelets
open WebSharper.UI.Next.Server

/// Server-side code.
module Server =

    [<Rpc>]
    let SendOrder(cmd, user, desc) =
        printfn "Send order: %s" cmd
        CentralAgent.agent.Post(CentralAgent.SendOrder(cmd, user, desc))

    [<Rpc>]
    let GetPlayerList() =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetPlayerList reply)

    [<Rpc>]
    let TryLogin(userPwd, coalitionPwd) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.TryLogin(userPwd, coalitionPwd, reply))

    [<Rpc>]
    let UpdateUserDb(forceTeamMessage) =
        CentralAgent.agent.Post(CentralAgent.UpdateUserDb forceTeamMessage)

    [<Rpc>]
    let FilterAvailable(platoons) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.FilterAvailable(platoons, reply))

    [<Rpc>]
    let TryGrab(user, platoon) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.TryGrab(user, platoon, reply))

    [<Rpc>]
    let Release(user, platoon) =
        CentralAgent.agent.Post(CentralAgent.Release(user, platoon))

    [<Rpc>]
    let UserControls(user, platoon) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.UserControls(user, platoon, reply))

    [<Rpc>]
    let GetUserPlatoons(user) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetUserPlatoons(user, reply))

    [<Rpc>]
    let Logout(user) =
        CentralAgent.agent.Post(CentralAgent.Logout user)


/// Client-side code.
[<JavaScript>]
module WebCommander =
    open WebSharper.JavaScript
    open WebSharper.UI.Next.Client

    type Speed =
        | Slow
        | Normal
        | Fast
    with
        static member Show(speed) =
            match speed with
            | Slow -> "slow speed"
            | Normal -> "normal speed"
            | Fast -> "maximum speed"

    type FireControl =
        | FreeFire
        | ReturnFire
        | HoldFire
    with
        static member Show(fire) =
            match fire with
            | FreeFire -> "free fire"
            | ReturnFire -> "return fire only"
            | HoldFire -> "hold fire"

    type Policy =
        { Speed : Speed
          FireControl : FireControl
        }

    type Order =
        | FormationStop
        | FormationContinue
        | FormationColumn
        | FormationOnRoad
        | FormationAttack
        | FlareGreen
        | FlareRed
        | Move of string
    with
        static member Show(order : Order) =
            match order with
            | FormationStop -> "Stop"
            | FormationContinue -> "Continue"
            | FormationColumn -> "Form off-road column"
            | FormationOnRoad -> "Form on-road column"
            | FormationAttack -> "Attack formation"
            | FlareGreen -> "Fire a green flare"
            | FlareRed -> "Fire a red flare"
            | Move dest -> dest

    type OrderAndPolicy =
        OrderAndPolicy of Order * Policy option
    with
        member this.ToServerInput(platoon, compressDestination) =
            match this with
            | OrderAndPolicy (FormationStop, _) -> sprintf "Order_%s_Stop" platoon
            | OrderAndPolicy (FormationContinue, _) -> sprintf "Order_%s_Continue" platoon
            | OrderAndPolicy (FormationColumn, _) -> sprintf "Order_%s_Column" platoon
            | OrderAndPolicy (FormationOnRoad, _) -> sprintf "Order_%s_OnRoad" platoon
            | OrderAndPolicy (FormationAttack, _) -> sprintf "Order_%s_Attack" platoon
            | OrderAndPolicy (FlareGreen, _) -> sprintf "Order_%s_GreenFlare" platoon
            | OrderAndPolicy (FlareRed, _) -> sprintf "Order_%s_RedFlare" platoon
            | OrderAndPolicy (Move dest, Some policy) ->
                let prefix1 =
                    match policy.FireControl with
                    | FreeFire -> "F"
                    | ReturnFire -> "R"
                    | HoldFire -> "H"
                let prefix2 =
                    match policy.Speed with
                    | Slow -> "S"
                    | Normal -> "N"
                    | Fast -> "F"
                sprintf "%s%s_%s_%s" prefix1 prefix2 platoon (compressDestination dest)
            | OrderAndPolicy (Move dest, None) ->
                OrderAndPolicy(Move dest, Some { Speed = Normal ; FireControl = ReturnFire }).ToServerInput(platoon, compressDestination)

        member this.Description(platoon) =
            match this with
            | OrderAndPolicy (FormationStop, _) -> sprintf "%s: Stop" platoon
            | OrderAndPolicy (FormationContinue, _) -> sprintf "%s: Continue" platoon
            | OrderAndPolicy (FormationColumn, _) -> sprintf "%s: Column" platoon
            | OrderAndPolicy (FormationOnRoad, _) -> sprintf "%s: On road" platoon
            | OrderAndPolicy (FormationAttack, _) -> sprintf "%s: Attack formation" platoon
            | OrderAndPolicy (FlareGreen, _) -> sprintf "%s: Fire green flare" platoon
            | OrderAndPolicy (FlareRed, _) -> sprintf "%s: Fire red flare" platoon
            | OrderAndPolicy (Move dest, Some policy) ->
                let prefix1 =
                    match policy.FireControl with
                    | FreeFire -> "free fire"
                    | ReturnFire -> "return fire"
                    | HoldFire -> "hold fire"
                let prefix2 =
                    match policy.Speed with
                    | Slow -> "slow"
                    | Normal -> "normal"
                    | Fast -> "fast"
                sprintf "%s: Move to %s at %s speed, %s" platoon dest prefix2 prefix1
            | OrderAndPolicy (Move dest, None) ->
                OrderAndPolicy(Move dest, Some { Speed = Normal ; FireControl = ReturnFire }).Description(platoon)


    type State =
        { User : Users.User option
          Coalition : Users.Coalition option
          Available : Users.Unit list
          Platoons : Users.Unit list
          LoginMessage : string option
          GrabMessage : string option
        }

    let TakeOrders(waypoints, axisPlatoons, alliedPlatoons) =
        let rvState =
            { User = None
              Coalition = None
              Available = []
              Platoons = []
              LoginMessage = None
              GrabMessage = None
            }
            |> Var.Create

        let commandSection (state : State) =
            let login =
                match state.User with
                | None ->
                    let coalitionPwd = Var.Create "AAA"
                    let userPwd = Var.Create "1234"
                    div [
                        div [
                            text "Your coalition code (3 letters) and your pin (4 digits):"
                            Doc.Input [] coalitionPwd
                            Doc.Input [] userPwd
                            text " "
                            Doc.Button "Log in" [] (fun () ->
                                async {
                                    let! userData = Server.TryLogin(userPwd.Value, coalitionPwd.Value.ToUpper())
                                    let user, coalition, status =
                                        match userData with
                                        | Some(Users.Named username as user, coalition) ->
                                            Some user, Some coalition, Some (sprintf "Welcome %s in coalition %s" username (coalition.AsString()))
                                        | None ->
                                            None, None, Some "Incorrect pin code"
                                    let! available =
                                        match coalition with
                                        | Some Users.Allies -> alliedPlatoons
                                        | Some Users.Axis -> axisPlatoons
                                        | None -> []
                                        |> Server.FilterAvailable
                                    Var.Set rvState { state with User = user; Coalition = coalition; Available = available; LoginMessage = status }
                                }
                                |> Async.Start
                            )
                            text " "
                            Doc.Button "Request PIN" [] (fun () ->
                                Server.UpdateUserDb(true)
                            )
                        ]
                        text (match state.LoginMessage with None -> "" | Some msg -> msg)
                    ] :> Doc
                | Some (Users.Named user) ->
                    Doc.TextNode <| sprintf "Command panel for %s" user

            let platoonSelection =
                match state.User, state.Coalition with
                | _, None
                | None, _ ->
                    Doc.Empty
                | Some user, Some coalition ->
                    match state.Available with
                    | [] ->
                        text "All platoons assigned"
                    | first :: _ ->
                        let chosen = Var.Create first
                        div [
                            Doc.Select [] (fun (x : Users.Unit) -> x.AsString()) state.Available chosen
                            Doc.ButtonView "Add" [] (View.FromVar chosen) (fun platoon ->
                                async {
                                    let! grabbed = Server.TryGrab(user, platoon)
                                    let! available =
                                        match coalition with
                                        | Users.Allies -> alliedPlatoons
                                        | Users.Axis -> axisPlatoons
                                        |> Server.FilterAvailable
                                    if grabbed then
                                        { state with
                                            Platoons = platoon :: state.Platoons
                                            GrabMessage = Some(sprintf "Platoon grabbed: %s" (platoon.AsString()))
                                            Available = available
                                        }
                                        |> Var.Set rvState
                                    else
                                        { state with
                                            GrabMessage = Some(sprintf "Could not grab control of %s" (platoon.AsString()))
                                            Available = available
                                        }
                                        |> Var.Set rvState
                                }
                                |> Async.Start
                            )
                        ] :> Doc

            let orderAssignment =
                let wpRank =
                    waypoints
                    |> List.mapi (fun i wp -> (wp, i))
                    |> List.fold (fun m (wp, i) -> Map.add wp i m) Map.empty

                let inline compressDestination wp =
                    sprintf "WP%d" (wpRank.[wp])

                let moveTo =
                    waypoints
                    |> List.sortBy (
                        function
                        | "North" -> (0, None)
                        | "North-East" -> (1, None)
                        | "East" -> (2, None)
                        | "South-East" -> (3, None)
                        | "South" -> (4, None)
                        | "South-West" -> (5, None)
                        | "West" -> (6, None)
                        | "North-West" -> (7, None)
                        | place -> (8, Some place)
                    )
                    |> List.map (fun name -> Move name )

                let orderList =
                    [ FormationOnRoad
                      FormationColumn
                      FormationAttack
                      //FlareGreen
                      //FlareRed
                    ]

                let speedList = [ Slow ; Fast ]
                let fireList = [ FreeFire ; ReturnFire ]

                match state.User with
                | Some user ->
                    let tryGiveOrder platoon (orderAndPolicy : OrderAndPolicy) =
                        async {
                            let! ok = Server.UserControls(user, platoon)
                            return!
                                if ok then
                                    let orderString = orderAndPolicy.ToServerInput(platoon.AsString(), compressDestination)
                                    Server.SendOrder(orderString, user, orderAndPolicy.Description(platoon.AsString()))
                                    async { return() }
                                else
                                    async {
                                        let! underControl = Server.GetUserPlatoons(user)
                                        Var.Set rvState {
                                            state with Platoons = underControl
                                        }
                                        return()
                                    }
                        }                    

                    let mkRow(platoon : Users.Unit) =
                        let chosenOrder = Var.Create (List.head orderList)
                        let chosenDestination = Var.Create (List.head moveTo)
                        let chosenSpeed = Var.Create (List.head speedList)
                        let chosenFire = Var.Create (List.head fireList)
                        div [
                            Doc.Button "X" [] (fun() ->
                                async {
                                    Server.Release(user, platoon)
                                    let! underControl = Server.GetUserPlatoons(user)
                                    let! available =
                                        match state.Coalition with
                                        | Some Users.Allies -> alliedPlatoons
                                        | Some Users.Axis -> axisPlatoons
                                        | None -> []
                                        |> Server.FilterAvailable
                                    Var.Set rvState {
                                        state with
                                            Platoons = underControl
                                            Available = available
                                    }
                                }
                                |> Async.Start
                            )
                            text " "
                            Doc.TextNode (platoon.AsString())
                            text " "
                            Doc.Select [] Order.Show orderList chosenOrder
                            text " "
                            Doc.Button "Order" [] (fun () ->
                                OrderAndPolicy(chosenOrder.Value, None)
                                |> tryGiveOrder platoon
                                |> Async.Start
                            )
                            text " - "
                            text "Move towards..."
                            Doc.Select [] Order.Show moveTo chosenDestination
                            text " at "
                            Doc.Select [] Speed.Show speedList chosenSpeed
                            text ", "
                            Doc.Select [] FireControl.Show fireList chosenFire
                            Doc.Button "Move" [] (fun () ->
                                OrderAndPolicy(chosenDestination.Value, Some { Speed = chosenSpeed.Value ; FireControl = chosenFire.Value })
                                |> tryGiveOrder platoon
                                |> Async.Start
                            )
                            text " - "
                            Doc.Button "Stop" [] (fun () ->
                                OrderAndPolicy(FormationStop, None)
                                |> tryGiveOrder platoon
                                |> Async.Start
                            )
                            text " "
                            Doc.Button "Continue" [] (fun () ->
                                OrderAndPolicy(FormationContinue, None)
                                |> tryGiveOrder platoon
                                |> Async.Start
                            )
                        ] :> Doc

                    div [
                        for platoon in state.Platoons do
                            yield mkRow platoon
                    ] :> Doc
                | None ->
                    Doc.Empty

            let logout =
                match state.User with
                | None ->
                    Doc.Empty
                | Some user ->
                    Doc.Button "Log out" [] (fun () ->
                        Server.Logout(user)
                        Var.Set rvState { state with User = None; Platoons = []; Coalition = None; LoginMessage = None }
                    ) :> Doc

            div [
                login
                platoonSelection
                orderAssignment
                logout
            ]

        View.Map commandSection (View.FromVar rvState)
        |> Doc.EmbedView

    let ShowResult(result) =
        result
        |> List.map (fun (k, v) -> sprintf "Key: %s Value: %s" k v)
        |> List.map text
        |> div

    type PlayerData =
        { Name : string
          Ping : int
        }

    let ShowPlayers(players : PlayerData[]) =
        table [
            yield tr [
                td [ text "Name" ] :> Doc
                td [ text "Ping" ] :> Doc
            ] :> Doc
            for player in players do
                yield tr [
                    td [ sprintf "%s" player.Name |> text] :> Doc
                    td [ sprintf "%3d" player.Ping |> text] :> Doc
                ] :> Doc
        ]


type EndPoint =
    | [<EndPoint "GET /">] Home
    | [<EndPoint "GET /players">] Players
    | [<EndPoint "GET /apiPlayerList">] ApiPlayers

/// <summary>
/// Build a WebSharper application.
/// </summary>
/// <param name="waypoints">Names of waypoints.</param>
/// <param name="platoons">Names of platoons.</param>
let MySite(waypoints, axisPlatoons, alliedPlatoons) =

    Application.MultiPage(fun ctx ->
        function
        | Home ->
            Content.Page(
                Title = "Orders",
                Body = [
                    div [ client <@ WebCommander.TakeOrders(waypoints, axisPlatoons, alliedPlatoons) @> ]
                ]
            )
        | Players ->
            async {
                let! players = CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetPlayerList reply)
                return!
                    match players with
                    | Some players ->
                        let players =
                            players
                            |> Array.map (fun data ->
                                { WebCommander.Name = data.Name
                                  WebCommander.Ping = data.Ping
                                })
                        Content.Page(
                            Title = "Player list",
                            Body = [
                                div [ client <@ WebCommander.ShowPlayers(players) @> ]
                            ]
                        )
                    | None ->
                        Content.Text "Failed to retrieve list of players from the server"
            }
        | ApiPlayers ->
            async {
                let! players = CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetPlayerList reply)
                return! Content.Json players
            }
    )

open SturmovikMissionTypes
open SturmovikMission.DataProvider.Mcu
open System.IO
open Suave.Web
open Suave.Types
open WebSharper.Suave

type T = Provider<"../../Lib/Sample.Mission", library="../../Lib/Sample.Mission">

let parseGroup filename =    
    let s = SturmovikMission.DataProvider.Parsing.Stream.FromFile(filename)
    T.GroupData(s)

[<EntryPoint>]
let main argv = 
    let config = Configuration.values

    let waypoints =
        (parseGroup config.WaypointsFilename).ListOfMCU_Waypoint
        |> List.map (fun wp -> wp.Name.Value)
        |> List.sort

    let axisPlatoons =
        (parseGroup config.PlatoonsFilename).ListOfVehicle
        |> List.filter(fun platoon -> platoon.Country.Value = 201)
        |> List.map (fun platoon -> Users.Platoon platoon.Name.Value)

    let alliedPlatoons =
        (parseGroup config.PlatoonsFilename).ListOfVehicle
        |> List.filter(fun platoon -> platoon.Country.Value = 101)
        |> List.map (fun platoon -> Users.Platoon platoon.Name.Value)

    let rec welcome() =
        async {
            CentralAgent.agent.Post(CentralAgent.UpdateUserDb false)
            printfn "Requested to update user DB"
            do! Async.Sleep 60000
            return! welcome()
        }

    try
        Async.Start(welcome())
        let myConfig = 
            { defaultConfig with
                logger =
                    if not Configuration.values.Logging then
                        defaultConfig.logger
                    else
                        upcast Suave.Logging.Loggers.ConsoleWindowLogger(Suave.Logging.LogLevel.Verbose)
                bindings =
                    Configuration.values.Bindings
                    |> Array.map (fun binding ->
                        match binding.Split(':') with
                        | [| ip; port |] ->
                            match System.Int32.TryParse(port) with
                            | true, port -> HttpBinding.mk' HTTP ip port
                            | false, _ -> failwithf "Port %s is not a valid port number" port 
                        | _ ->
                            failwithf "Ill-formed binding '%s'. Should be of the form ip:port" binding
                    )
                    |> List.ofArray
            }
        startWebServer myConfig (WebSharperAdapter.ToWebPart <| MySite(waypoints, axisPlatoons, alliedPlatoons))
        0
    with
        | exc ->
            failwithf "Exception: %s" exc.Message
            1
