// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
module Commander

open System
open System.Net
open System.Text

/// <summary>
/// Interaction between the web server and DServer.
/// Provides authentication, triggering server inputs...
/// </summary>
type Client(address : IPAddress, port, login, password) as this =
    inherit Sockets.TcpClient()

    do base.Connect(address, port)
    let stream = this.GetStream()

    let send(buff, idx, len) = Async.FromBeginEnd((fun (cb, par) -> stream.BeginWrite(buff, idx, len, cb, par)), stream.EndWrite)
    let receive(buff, idx, len) = Async.FromBeginEnd((fun (cb, par) -> stream.BeginRead(buff, idx, len, cb, par)), stream.EndRead)

    let encode (cmd : string) =
        let asAscii = Encoding.ASCII.GetBytes(cmd)
        let length = [| byte((asAscii.Length + 1) % 256) ; byte((asAscii.Length + 1) / 256) |]
        let buff = Array.concat [length ; asAscii ; [|0uy|] ]
        buff

    let getResponse(stream : Sockets.NetworkStream) =
        async {
            let buffer : byte[] = Array.zeroCreate 0xffff
            let response = stream.Read(buffer, 0, 2)
            let responseLength = (256 * int buffer.[1]) + int buffer.[0]
            let! response = receive(buffer, 2, responseLength)
            let data = Encoding.ASCII.GetString(buffer, 2, responseLength - 1)
            return data
        }

    member this.Auth() =
        async {
            let buff = encode <| sprintf "auth %s %s" login password
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

    member this.ServerInput(name) =
        async {
            let buff =
                sprintf "serverinput %s" name
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return response
        }

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.Sitelets
open WebSharper.UI.Next.Server

/// Server-side code.
module Server =
    [<Rpc>]
    let SendOrder cmd =
        async {
            let config = Configuration.values
            let rconIP = IPAddress.Parse(config.RconAddress)
            use client = new Client(rconIP, config.RconPort, config.RconUsername, config.RconPassword)
            let! _ = client.Auth()
            let! _ = client.ServerInput(cmd)
            return ()
        }

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

    let orders(waypoints, platoons) =
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

        let chosenOrder = Var.Create FormationStop
        let chosenPlatoon = Var.Create (List.head platoons)

        div [
            text "Your order: "
            Doc.Select [] id platoons chosenPlatoon
            Doc.Select [] Order.Show orderList chosenOrder
            Doc.Button "Send" [] (fun () ->
                Server.SendOrder(chosenOrder.Value.ToServerInput(chosenPlatoon.Value))
                |> Async.Start
            )
        ]

/// <summary>
/// Build a WebSharper application.
/// </summary>
/// <param name="waypoints">Names of waypoints.</param>
/// <param name="platoons">Names of platoons.</param>
let MySite(waypoints, platoons) =
    Application.SinglePage(fun ctx ->
        Content.Page(
            Body = [
                div [ client <@ WebCommander.orders(waypoints, platoons) @> ]
            ]
        ))

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

    WebSharper.Warp.RunAndWaitForInput(MySite(waypoints, platoons), urls = List.ofArray config.WebListeningAddresses)
