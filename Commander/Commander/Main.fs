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
    let RequestPin(userName) =
        CentralAgent.agent.Post(CentralAgent.RequestPin userName)

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

    [<Rpc>]
    let AuthenticateMaster(pwd) =
        async {
            match Configuration.values.Events with
            | Some events ->
                return events.Password = pwd
            | None ->
                return false
        }

    [<Rpc>]
    let SendServerInput(pwd, input) =
        CentralAgent.agent.Post(CentralAgent.SendServerInput(pwd, input))


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
          Platoons : Users.Unit list
        }

    let TakeOrders(waypoints, axisPlatoons, alliedPlatoons) =
        let rvState =
            { User = None
              Coalition = None
              Platoons = []
            }
            |> Var.Create
        let rvAvailable : Var<Users.Unit list> = Var.Create []
        let rvMessage = Var.Create "Welcome to Coconut's 'Ground Commander' for IL-2 Sturmovik: Battle Of Stalingrad"

        let loginSection (state : State) =
            match state.User with
            | None ->
                let coalitionPwd = Var.Create "AAA"
                let userPwd = Var.Create "1234"
                let userName = Var.Create "playerName"
                div [
                    div [
                        text "Request PIN "
                        Doc.Input [] userName
                        Doc.ButtonView "Send PIN to my in-game chat" [] (View.FromVar userName) Server.RequestPin
                    ]
                    div [                        
                        text "Your coalition code (3 letters) and your PIN (4 digits):"
                        Doc.Input [] coalitionPwd
                        Doc.Input [] userPwd
                        text " "
                        Doc.Button "Log in" [] (fun () ->
                            async {
                                let! userData = Server.TryLogin(userPwd.Value, coalitionPwd.Value.ToUpper())
                                let user, coalition, status =
                                    match userData with
                                    | Some(Users.Named username as user, coalition) ->
                                        Some user, Some coalition, sprintf "Welcome %s in coalition %s" username (coalition.AsString())
                                    | None ->
                                        None, None, "Incorrect coalition code or PIN"
                                let! available =
                                    match coalition with
                                    | Some Users.Allies -> alliedPlatoons
                                    | Some Users.Axis -> axisPlatoons
                                    | None -> []
                                    |> Server.FilterAvailable
                                rvState.Value <- { state with User = user; Coalition = coalition }
                                rvAvailable.Value <- available
                                rvMessage.Value <- status
                            }
                            |> Async.Start
                        )
                    ]
                ] :> Doc
            | Some user ->
                Doc.Button "Log out" [] (fun () ->
                    Server.Logout(user)
                    rvState.Value <- { state with User = None; Platoons = []; Coalition = None }
                    rvMessage.Value <- ""
                ) :> Doc

        let platoonSelection (state : State) (available : Users.Unit list) =
            match state.User, state.Coalition with
            | _, None
            | None, _ ->
                Doc.Empty
            | Some user, Some coalition ->
                match available with
                | [] ->
                    div [ text "All platoons assigned" ] :> Doc
                | first :: _ ->
                    let chosen = Var.Create first
                    div [
                        Doc.Button "Update" [] (fun () ->
                            async {
                                let! available =
                                    match coalition with
                                    | Users.Allies -> alliedPlatoons
                                    | Users.Axis -> axisPlatoons
                                    |> Server.FilterAvailable
                                rvAvailable.Value <- available
                            }
                            |> Async.Start
                        )
                        text " "
                        Doc.Select [] (fun (x : Users.Unit) -> x.AsString()) available chosen
                        text " "
                        Doc.ButtonView "Add" [] (View.FromVar chosen) (fun platoon ->
                            async {
                                let! grabbed = Server.TryGrab(user, platoon)
                                if grabbed then
                                    rvState.Value <-
                                        { state with
                                            Platoons = platoon :: state.Platoons
                                        }
                                    rvMessage.Value <- sprintf "Platoon grabbed: %s" (platoon.AsString())
                                else
                                    rvMessage.Value <- sprintf "Could not grab control of %s" (platoon.AsString())
                            }
                            |> Async.Start
                        )
                    ] :> Doc

        let orderAssignment (state : State) =
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
                        if ok then
                            let orderString = orderAndPolicy.ToServerInput(platoon.AsString(), compressDestination)
                            let orderDesc = orderAndPolicy.Description(platoon.AsString())
                            Server.SendOrder(orderString, user, orderDesc)
                            rvMessage.Value <- orderDesc
                        else
                            rvMessage.Value <- "Failed to send order"
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
                                let underControl =
                                    state.Platoons
                                    |> List.filter ((<>) platoon)
                                let! available =
                                    match state.Coalition with
                                    | Some Users.Allies -> alliedPlatoons
                                    | Some Users.Axis -> axisPlatoons
                                    | None -> []
                                    |> Server.FilterAvailable
                                rvState.Value <- {
                                    state with
                                        Platoons = underControl
                                }
                                rvAvailable.Value <- available
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
        div [
            textView (View.FromVar rvMessage)
            View.Map loginSection (View.FromVar rvState) |> Doc.EmbedView
            View.Map2 platoonSelection (View.FromVar rvState) (View.FromVar rvAvailable) |> Doc.EmbedView
            View.Map orderAssignment (View.FromVar rvState) |> Doc.EmbedView
        ]

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

[<JavaScript>]
module GameEvents =
    open WebSharper.JavaScript
    open WebSharper.UI.Next.Client

    type State =
        { Authenticated : bool
          Password : string
        }
    with
        static member Default = { Authenticated = false; Password = "" }

    let showEvents(events : (string * string) list) =
        let rvState = Var.Create State.Default
        let loginSection(state) =
            match state with
            | { Authenticated = false } ->
                let pwd = Var.Create state.Password
                div [
                    text "Password: "
                    Doc.Input [] pwd
                    text " "
                    Doc.Button "Send" [] (fun () ->
                        async {
                            let! auth = Server.AuthenticateMaster pwd.Value
                            rvState.Value <- {
                                state with
                                    Authenticated = auth
                                    Password = pwd.Value
                            }
                        }
                        |> Async.Start
                    )
                ]
            | { Authenticated = true } ->
                div [
                    Doc.Button "Log out" [] (fun () ->
                        rvState.Value <- State.Default
                    )
                ]
        let eventsSection(state) =
            match state, events with
            | { Authenticated = false }, _ ->
                Doc.Empty
            | { Authenticated = true }, event :: _ ->
                let item = Var.Create event
                div [
                    Doc.Select [] snd events item
                    text " "
                    Doc.Button "Send" [] (fun () ->
                        async {
                            Server.SendServerInput(state.Password, fst item.Value)
                        }
                        |> Async.Start
                    ) 
                ] :> Doc
            | _, [] ->
                Doc.Empty
        div [
            View.Map loginSection (View.FromVar rvState) |> Doc.EmbedView
            View.Map eventsSection (View.FromVar rvState) |> Doc.EmbedView
        ]

type EndPoint =
    | [<EndPoint "GET /commands">] Commands
    | [<EndPoint "GET /master">] Master
    | [<EndPoint "GET /apiPlayerList">] ApiPlayers

/// <summary>
/// Build a single-page application that shows the interface that allows users
/// to grab platoons and give them orders.
/// </summary>
/// <param name="waypoints">Names of waypoints.</param>
/// <param name="platoons">Names of platoons.</param>
let mkCommandsPage(waypoints, axisPlatoons, alliedPlatoons) : Async<Content<EndPoint>> =
    Content.Page(
        Title = "Orders",
        Body = [
            div [ client <@ WebCommander.TakeOrders(waypoints, axisPlatoons, alliedPlatoons) @> ]
        ]
    )

let mkNotAvailablePage() : Async<Content<EndPoint>> =
    Content.Page(text "Not available")

let mkEventsPage(events) : Async<Content<EndPoint>> =
    Content.Page(
        Title = "Game events",
        Body = [
            div [ client <@ GameEvents.showEvents(events) @> ]
        ])

/// <summary>
/// Build a WebSharper application.
/// </summary>
let MySite(getCommandsPage, getMasterPage) =
    Application.MultiPage(fun ctx ->
        function
        | Commands ->
            getCommandsPage()
        | Master ->
            getMasterPage()
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

    let rec welcome() =
        async {
            CentralAgent.agent.Post(CentralAgent.UpdateUserDb false)
            printfn "Requested to update user DB"
            do! Async.Sleep 60000
            return! welcome()
        }

    try
        let myConfig = 
            { defaultConfig with
                logger =
                    match Configuration.values.Logging with
                    | None
                    | Some false ->
                        defaultConfig.logger
                    | Some true ->
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
        let getCommandsPage =
            match config.Armies with
            | Some armies ->
                Async.Start(welcome())
                let waypoints =
                    (parseGroup armies.WaypointsFilename).ListOfMCU_Waypoint
                    |> List.map (fun wp -> wp.Name.Value)
                    |> List.sort
                let platoonsGroupVehicles =
                    (parseGroup armies.PlatoonsFilename).ListOfVehicle
                let axisPlatoons =
                    platoonsGroupVehicles
                    |> List.filter(fun platoon -> platoon.Country.Value = 201)
                    |> List.map (fun platoon -> Users.Platoon platoon.Name.Value)
                let alliedPlatoons =
                    platoonsGroupVehicles
                    |> List.filter(fun platoon -> platoon.Country.Value = 101)
                    |> List.map (fun platoon -> Users.Platoon platoon.Name.Value)
                fun () -> mkCommandsPage(waypoints, axisPlatoons, alliedPlatoons)
            | None ->
                mkNotAvailablePage
        let getMasterPage =
            match config.Events with
            | Some events ->
                let items =
                    events.Items
                    |> Seq.map (fun item -> item.Name, item.Label)
                    |> List.ofSeq
                fun () ->
                    mkEventsPage items
            | None ->
                mkNotAvailablePage
        startWebServer myConfig (WebSharperAdapter.ToWebPart <| MySite(getCommandsPage, getMasterPage))
        0
    with
        | exc ->
            failwithf "Exception: %s" exc.Message
            1
