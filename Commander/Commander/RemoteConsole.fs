module RemoteConsole

open System.Net
open System.Text
open System.Web
open System
open WebSharper

[<JavaScript>]
type PlayerData =
    { ClientId : int
      Status : int
      Ping : int
      Name : string
      PlayerId : string
      ProfileId : string
    }

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
            let data =
                if responseLength > 0 then
                    Encoding.ASCII.GetString(buffer, 2, responseLength - 1)
                else
                    ""
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

    member this.GetPlayerList() =
        async {
            let buff = encode "getplayerlist"
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            let pairs = response.Split('&')
            let result =
                pairs
                |> Array.map (fun kvp ->
                    match kvp.Split('=') with
                    | [| key; value |] -> (key, HttpUtility.UrlDecode(value))
                    | _ -> failwithf "Ill-formatted key-value pair %s" kvp)
                |> Array.tryPick (function
                    | "playerList", values ->
                        HttpUtility.UrlDecode(values).Split('|')
                        |> Array.map (fun player -> player.Split(','))
                        |> Some
                    | _ ->
                        None)
                |> Option.map (fun rows ->
                    rows.[1..]
                    |> Array.map(fun row ->
                        { ClientId = Int32.Parse(row.[0])
                          Status = Int32.Parse(row.[1])
                          Ping = Int32.Parse(row.[2])
                          Name = row.[3]
                          PlayerId = row.[4]
                          ProfileId = row.[5]
                        })
                    |> Array.filter(fun data -> data.ClientId <> 0)
                )
            return result
        }

    member this.MessagePlayer(playerId, msg) =
        async {
            let buff =
                sprintf "chatmsg 3 %d %s" playerId msg
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return()
        }

    member this.MessageTeam(teamId, msg) =
        async {
            let buff =
                sprintf "chatmsg 1 %d %s" teamId msg
                |> encode
            do! send(buff, 0, buff.Length)
            let! response = getResponse stream
            return()
        }
