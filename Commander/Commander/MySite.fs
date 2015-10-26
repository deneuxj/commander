// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
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
        let context = WebSharper.Web.Remoting.GetContext()
        CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.TryLogin(password, context, reply))

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

    let TakeOrders(waypoints, platoons) =
        let wpRank =
            waypoints
            |> List.mapi (fun i wp -> (wp, i))
            |> List.fold (fun m (wp, i) -> Map.add wp i m) Map.empty

        let compressDestination wp =
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

        let speedList = [ Slow ; Normal ; Fast ]
        let fireList = [ FreeFire ; ReturnFire ; HoldFire ]

        let mkRow(defaultPlatoon) =
            let chosenOrder = Var.Create (List.head orderList)
            let chosenDestination = Var.Create (List.head moveTo)
            let chosenSpeed = Var.Create (List.head speedList)
            let chosenFire = Var.Create (List.head fireList)
            let chosenPlatoon = Var.Create (List.nth platoons defaultPlatoon)
            div [
                Doc.Select [] id platoons chosenPlatoon
                Doc.Select [] Order.Show orderList chosenOrder
                Doc.Button "Order" [] (fun () ->
                    let orderAndPolicy =
                        OrderAndPolicy(chosenOrder.Value, None)
                    Server.SendOrder(orderAndPolicy.ToServerInput(chosenPlatoon.Value, compressDestination))
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
                    Server.SendOrder(orderAndPolicy.ToServerInput(chosenPlatoon.Value, compressDestination))
                )
                text " - "
                Doc.Button "Stop" [] (fun () ->
                    Server.SendOrder(OrderAndPolicy(FormationStop, None).ToServerInput(chosenPlatoon.Value, compressDestination))
                )
                text " "
                Doc.Button "Continue" [] (fun () ->
                    Server.SendOrder(OrderAndPolicy(FormationContinue, None).ToServerInput(chosenPlatoon.Value, compressDestination))
                )
            ] :> Doc

        let numRows =
            min (List.length platoons) 6
        div [
            for i in 0..numRows-1 do
                yield mkRow i
        ]

    let ShowResult(result) =
        result
        |> List.map (fun (k, v) -> sprintf "Key: %s Value: %s" k v)
        |> List.map text
        |> div

    let ShowPlayers(players : RemoteConsole.PlayerData[]) =
        table [
            yield tr [
                td [ text "Client ID" ] :> Doc
                td [ text "Status" ] :> Doc
                td [ text "Name" ] :> Doc
            ] :> Doc
            for player in players do
                yield tr [
                    td [ sprintf "%02d" player.ClientId |> text ] :> Doc
                    td [ sprintf "%d" player.Status |> text] :> Doc
                    td [ sprintf "%s" player.Name |> text] :> Doc
                ] :> Doc
        ]

    let Login() =
        let password = Var.Create "AAA1234"
        let status = Var.Create "Please enter the pin code provided to you in the game chat"
        div [
            div [
                text "Your password: "
                Doc.Input [] password
                Doc.Button "Log in" [] (fun () ->
                    async {
                        let! username = Server.TryLogin(password.Value)
                        match username with
                        | Some username ->
                            Var.Set status (sprintf "Welcome %s" username)
                        | None ->
                            Var.Set status "Incorrect pin code"
                    }
                    |> Async.Start
                )
            ]
            Doc.TextView (View.FromVar status)
        ]

type EndPoint =
    | [<EndPoint "GET /login">] Login
    | [<EndPoint "GET /">] Home
    | [<EndPoint "GET /players">] Players
    | [<EndPoint "GET /apiPlayerList">] ApiPlayers
    | [<EndPoint "GET /orders">] Orders

/// <summary>
/// Build a WebSharper application.
/// </summary>
/// <param name="waypoints">Names of waypoints.</param>
/// <param name="platoons">Names of platoons.</param>
let MySite(waypoints, platoons) =
    let menu (ctx : Context<_>) =
        div [
            aAttr [ Attr.Create "href" (ctx.Link Home) ] [ text "Home" ]
            text " - "
            aAttr [ Attr.Create "href" (ctx.Link Login) ] [ text "Log in" ]
            text " - "
            aAttr [ Attr.Create "href" (ctx.Link Orders) ] [ text "Orders" ]
        ]

    let login ctx =
        async {
            Server.UpdateUserDb()
            return!
                Content.Page(
                    Body = [
                        menu ctx
                        div [ client <@ WebCommander.Login() @> ]
                    ]
                )
        }

    Application.MultiPage(fun ctx ->
        function
        | Home ->
            Content.Page(
                Title = "IL-2 Sturmovik Ground Forces Commander",
                Body = [ menu ctx ]
            )
        | Login ->
            login ctx
        | Orders ->
            async {
                let! user = ctx.UserSession.GetLoggedInUser()
                match user with
                | None
                | Some _ ->
                    return!
                        Content.Page(
                            Title = "Orders",
                            Body = [
                                menu ctx
                                div [ client <@ WebCommander.TakeOrders(waypoints, platoons) @> ]
                            ]
                        )
                | None ->
                    return!
                        Content.RedirectTemporary Login
            }
        | Players ->
            async {
                let! players = CentralAgent.agent.PostAndAsyncReply(fun reply -> CentralAgent.GetPlayerList reply)
                return!
                    match players with
                    | Some players ->
                        Content.Page(
                            Title = "Player list",
                            Body = [
                                menu ctx
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
            do! Async.Sleep 60000
            return! welcome()
        }

    Async.Start(welcome())
    WebSharper.Warp.RunAndWaitForInput(MySite(waypoints, platoons), urls = List.ofArray config.WebListeningAddresses)
