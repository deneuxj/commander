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
    let SendOrder cmd =
        printfn "Order sent: %s" cmd
        CentralAgent.agent.Post (CentralAgent.SendOrder cmd)

    [<Rpc>]
    let GetPlayerList() =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetPlayerList reply)

    [<Rpc>]
    let TryLogin(password) =
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.TryLogin(password, reply))

    [<Rpc>]
    let UpdateUserDb() =
        CentralAgent.agent.Post (CentralAgent.UpdateUserDb)

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

    type State =
        { User : string option
          Platoons : string list
          LoginMessage : string option
        }

    let TakeOrders(waypoints, platoons) =
        let rvState =
            { User = None
              Platoons = []
              LoginMessage = None
            }
            |> Var.Create

        let commandSection (state : State) =
            let login =
                match state.User with
                | None ->
                    let password = Var.Create "AAA1234"
                    div [
                        div [
                            text "Your PIN: "
                            Doc.Input [] password
                            Doc.Button "Log in" [] (fun () ->
                                async {
                                    let! username = Server.TryLogin(password.Value)
                                    let status =
                                        match username with
                                        | Some username ->
                                            Some (sprintf "Welcome %s" username)
                                        | None ->
                                            Some "Incorrect pin code"
                                    Var.Set rvState { state with User = username; LoginMessage = status }
                                }
                                |> Async.Start
                            )
                            Doc.Button "Request PIN" [] (fun () ->
                                Server.UpdateUserDb()
                            )
                        ]
                        text (match state.LoginMessage with None -> "" | Some msg -> msg)
                    ] :> Doc
                | Some user ->
                    Doc.TextNode <| sprintf "Command panel for %s" user

            let platoonSelection =
                match state.User with
                | None ->
                    Doc.Empty
                | Some user ->
                    let available =
                        platoons
                        |> List.filter(fun platoon -> List.contains platoon state.Platoons |> not)
                    match available with
                    | [] ->
                        text "All platoons assigned"
                    | first :: _ ->
                        let chosen = Var.Create first
                        div [
                            Doc.Select [] id available chosen
                            Doc.ButtonView "Add" [] (View.FromVar chosen) (fun platoon ->
                                let state2 = { state with Platoons = platoon :: state.Platoons }
                                Var.Set rvState state2
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
                      FlareGreen
                      FlareRed
                    ]

                let speedList = [ Slow ; Fast ]
                let fireList = [ FreeFire ; ReturnFire ]

                let mkRow(platoon) =
                    let chosenOrder = Var.Create (List.head orderList)
                    let chosenDestination = Var.Create (List.head moveTo)
                    let chosenSpeed = Var.Create (List.head speedList)
                    let chosenFire = Var.Create (List.head fireList)
                    div [
                        Doc.TextNode platoon
                        Doc.Select [] Order.Show orderList chosenOrder
                        Doc.Button "Order" [] (fun () ->
                            let orderAndPolicy =
                                OrderAndPolicy(chosenOrder.Value, None)
                            Server.SendOrder(orderAndPolicy.ToServerInput(platoon, compressDestination))
                        )
                        text " - "
                        text "Move towards..."
                        Doc.Select [] Order.Show moveTo chosenDestination
                        text " at "
                        Doc.Select [] Speed.Show speedList chosenSpeed
                        text ", "
                        Doc.Select [] FireControl.Show fireList chosenFire
                        Doc.Button "Move" [] (fun () ->
                            let orderAndPolicy =
                                OrderAndPolicy(chosenDestination.Value, Some { Speed = chosenSpeed.Value ; FireControl = chosenFire.Value })
                            Server.SendOrder(orderAndPolicy.ToServerInput(platoon, compressDestination))
                        )
                        text " - "
                        Doc.Button "Stop" [] (fun () ->
                            Server.SendOrder(OrderAndPolicy(FormationStop, None).ToServerInput(platoon, compressDestination))
                        )
                        text " "
                        Doc.Button "Continue" [] (fun () ->
                            Server.SendOrder(OrderAndPolicy(FormationContinue, None).ToServerInput(platoon, compressDestination))
                        )
                    ] :> Doc

                div [
                    for platoon in state.Platoons do
                        yield mkRow platoon
                ]

            let logout =
                match state.User with
                | None ->
                    Doc.Empty
                | Some user ->
                    Doc.Button "Log out" [] (fun () ->
                        Var.Set rvState { state with User = None; Platoons = [] }
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
let MySite(waypoints, platoons) =

    Application.MultiPage(fun ctx ->
        function
        | Home ->
            Content.Page(
                Title = "Orders",
                Body = [
                    div [ client <@ WebCommander.TakeOrders(waypoints, platoons) @> ]
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

type T = Provider<"Sample.Mission", library="Sample.Mission">

let parseGroup filename =
    let s = SturmovikMission.DataProvider.Parsing.Stream.FromFile(Path.Combine(T.ResolutionFolder, filename))
    T.GroupData(s)

[<EntryPoint>]
let main argv = 
    let config = Configuration.values

    let waypoints =
        (parseGroup config.WaypointsFilename).ListOfMCU_Waypoint
        |> List.map (fun wp -> wp.Name.Value)
        |> List.sort

    let platoons =
        (parseGroup config.PlatoonsFilename).ListOfVehicle
        |> List.map (fun platoon -> platoon.Name.Value)

    let rec welcome() =
        async {
            CentralAgent.agent.Post(CentralAgent.UpdateUserDb)
            printfn "Requested to update user DB"
            do! Async.Sleep 60000
            return! welcome()
        }

    try
        Async.Start(welcome())
        let myConfig = 
            { defaultConfig with
                //logger = Suave.Logging.Loggers.ConsoleWindowLogger(Suave.Logging.LogLevel.Verbose)
                bindings =
                [
                    HttpBinding.mk' HTTP "192.168.0.100" 9000
                    HttpBinding.mk' HTTP "127.0.0.1" 9000
                ]
            }
        startWebServer myConfig (WebSharperAdapter.ToWebPart <| MySite(waypoints, platoons))
        0
    with
        | exc ->
            failwithf "Exception: %s" exc.Message
            1
