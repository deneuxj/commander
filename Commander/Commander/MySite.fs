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

    type Destination =
        { Destination : string }

    type Order =
        | FormationStop
        | FormationContinue
        | FormationColumn
        | FormationOnRoad
        | FormationAttack
        | FlareGreen
        | FlareRed
        | Attack of Destination
        | Move of Destination

    type Order with
        static member Show(order : Order) =
            match order with
            | FormationStop -> "Stop"
            | FormationContinue -> "Continue"
            | FormationColumn -> "Form off-road column"
            | FormationOnRoad -> "Form on-road column"
            | FormationAttack -> "Attack formation"
            | FlareGreen -> "Fire a green flare"
            | FlareRed -> "Fire a red flare"
            | Attack dest -> sprintf "Move towards %s, free fire" dest.Destination
            | Move dest -> sprintf "Move towards %s, return fire only" dest.Destination

        member this.ToServerInput(platoon) =
            match this with
            | FormationStop -> sprintf "Order_%s_Stop" platoon
            | FormationContinue -> sprintf "Order_%s_Continue" platoon
            | FormationColumn -> sprintf "Order_%s_Column" platoon
            | FormationOnRoad -> sprintf "Order_%s_OnRoad" platoon
            | FormationAttack -> sprintf "Order_%s_Attack" platoon
            | FlareGreen -> sprintf "Order_%s_GreenFlare" platoon
            | FlareRed -> sprintf "Order_%s_RedFlare" platoon
            | Attack dest -> sprintf "Attack_%s_%s" platoon (dest.Destination.Replace(" ", "_"))
            | Move dest -> sprintf "Move_%s_%s" platoon (dest.Destination.Replace(" ", "_"))

    let TakeOrders(waypoints, platoons) =
        let attackTo =
            waypoints
            |> List.map (fun name -> Attack { Destination = name })

        let moveTo =
            waypoints
            |> List.map (fun name -> Move { Destination = name })

        let orderList =
            [ FormationOnRoad
              FormationStop
              FormationContinue
              FormationColumn
              FormationAttack
              FlareGreen
              FlareRed
            ] @ attackTo @ moveTo

        let numRows = 8
        let chosenOrders = Array.init numRows (fun _ -> Var.Create FormationStop)        
        let myPlatoons =
            let numPlatoons = List.length platoons
            Array.init numRows (fun i -> Var.Create <| List.nth platoons (max i (numPlatoons - 1)))
        div [
            for platoon, order in Array.zip myPlatoons chosenOrders do
                yield div [
                    Doc.Select [] id platoons platoon
                    Doc.Select [] Order.Show orderList order
                    Doc.Button "Send" [] (fun () ->
                        Server.SendOrder(order.Value.ToServerInput(platoon.Value))
                    )
                    Doc.Button "Stop" [] (fun () ->
                        Server.SendOrder(FormationStop.ToServerInput(platoon.Value))
                    )
                    Doc.Button "Continue" [] (fun () ->
                        Server.SendOrder(FormationContinue.ToServerInput(platoon.Value))
                    )
                ] :> Doc
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
