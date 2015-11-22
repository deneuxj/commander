#I __SOURCE_DIRECTORY__
#r @"Commander\packages\SturmovikMission.DataProvider.1.3.0.1\lib\net45\DataProvider.dll"

open System.IO

open SturmovikMissionTypes
open SturmovikMission
open SturmovikMission.DataProvider
open SturmovikMission.DataProvider.Mcu
open SturmovikMission.DataProvider.McuUtil
open SturmovikMission.DataProvider.NumericalIdentifiers
open System.Text.RegularExpressions

type T = Provider<"Lib/Sample.Mission", "Lib/GerPz3.Group;Lib/GerPz4.Group;Lib/GerMechanizedInfantry.Group;Lib/GerMobileArtillery.Group;Lib/Palette.Group;Lib/Rus52k.Group;Lib/Rus61k.Group;Lib/RusAT.Group">

/// Create a vector from any value that has fields XPos, YPos and ZPos.
let inline getPos< ^T when
                    ^T : (member XPos : T.Float)
                    and ^T : (member YPos : T.Float)
                    and ^T : (member ZPos : T.Float) > (v : ^T) =
    let x = (^T : (member XPos : T.Float) v)
    let y = (^T : (member YPos : T.Float) v)
    let z = (^T : (member ZPos : T.Float) v)
    newVec3(x.Value, y.Value, z.Value)

let (@@) (xs1 : #McuBase list) (xs2 : McuBase list) =
    (xs1 |> List.map (fun x -> x :> McuBase)) @ xs2

let cleanSpaces (s : string) =
    s.Replace(" ", "_")

let (|StartsWith|_|) prefix (s : string) =
    if s.StartsWith(prefix) then
        Some()
    else
        None

let (|EndsWith|_|) prefix (s : string) =
    if s.EndsWith(prefix) then
        Some()
    else
        None

let mkInput (subst : McuBase seq -> int -> int) name prefix (cmd : McuCommand) =
    let input =
        T.Palette.AnInput
            .SetName(T.String (sprintf "%s_%s_%s" prefix name (cleanSpaces cmd.Name)))
            .SetXPos(T.Float cmd.Pos.X)
            .SetYPos(T.Float cmd.Pos.Y)
            .SetZPos(T.Float (cmd.Pos.Z + 50.0))
            .CreateMcuCommand()
    subst [input] |> ignore
    input.Targets <- [cmd.Index]
    input

type WaypointPriority =
    | Low = 0
    | Medium = 1
    | High = 2

type TravelSpeed =
    | Slow = 0
    | Normal = 1
    | Fast = 2

let tryGetRussianBaseEquivalent =
    function
    | "horch830" -> Some ("gaz", "gaz-m")
    | "opel-blitz" -> Some ("zis", "zis5")
    | "pziii-l" -> Some ("t34-76stz", "t34-76stz")
    | "pziv-g" -> Some ("t70", "t70")
    | "sdkfz10-flak38" -> Some ("gaz", "gaz-aa-m4-aa")
    | "sdkfz251-1c" -> Some ("ba64", "ba64")
    | "sdkfz251-szf" -> Some ("zis", "bm13")
    | "stug37l24" -> Some ("kv1-42", "kv1-42")
    | "stug40l43" -> Some ("kv1-42", "kv1-42")
    | _ -> None

let getRussianScriptEquivalent script =
    let re = Regex(@"LuaScripts\\WorldObjects\\vehicles\\(.*)\.txt")
    let m = re.Match(script)
    if m.Success then
        match tryGetRussianBaseEquivalent m.Groups.[1].Value with
        | Some (_, ru) -> sprintf @"LuaScripts\WorldObjects\vehicles\%s.txt" ru
        | None -> script
    else
        script

let getRussianModelEquivalent model =
    let re = Regex(@"graphics\\vehicles\\(.*)\\(.*)\.mgm")
    let m = re.Match(model)
    if m.Success then
        match tryGetRussianBaseEquivalent m.Groups.[2].Value with
        | Some (sub, ru) -> sprintf @"graphics\vehicles\%s\%s.mgm" sub ru
        | None -> model
    else
        model

let tryGetGermanArtilleryBaseEquivalent =
    function
    | "zis3gun" -> Some("pak40", "pak40")
    | "52k" -> Some("flak37", "flak37")
    | "61k" -> Some("flak38", "flak38")
    | _ -> None

let getGermanArtilleryScriptEquivalent script =
    let re = Regex(@"LuaScripts\\WorldObjects\\vehicles\\(.*)\.txt")
    let m = re.Match(script)
    if m.Success then
        match tryGetGermanArtilleryBaseEquivalent m.Groups.[1].Value with
        | Some (_, ger) -> sprintf @"LuaScripts\WorldObjects\vehicles\%s.txt" ger
        | None -> script
    else
        script

let getGermanArtilleryModelEquivalent model =
    let re = Regex(@"graphics\\artillery\\(.*)\\(.*)\.mgm")
    let m = re.Match(model)
    if m.Success then
        match tryGetGermanArtilleryBaseEquivalent m.Groups.[2].Value with
        | Some (sub, ger) -> sprintf @"graphics\artillery\%s\%s.mgm" sub ger
        | None -> model
    else
        model

type VehicleGroup =
    { Leader : HasEntity
      All : McuBase list
    }
with
    interface IMcuGroup with
        member this.Content = this.All
        member this.SubGroups = []
        member this.LcStrings = []

    static member Create(subst : McuBase seq -> int -> int, name : string, pos : Vec3, ori : float, swapToRussians : bool, speed : int, newVehicles : unit -> McuBase list, getLeader : McuBase list -> HasEntity, waypoints : T.MCU_Waypoint list) =
        let wpRank =
            waypoints
            |> List.mapi (fun i wp -> (wp.Name.Value, i))
            |> List.fold (fun m (wp, i) -> Map.add wp i m) Map.empty

        let getSpeedValue =
            function
            | TravelSpeed.Slow -> 20
            | TravelSpeed.Normal -> 2 * speed / 3
            | TravelSpeed.Fast -> speed
            | _ -> speed

        let createWaypoint (prio : WaypointPriority) (speed : TravelSpeed) (pos : Vec3) name =
            let res =
                T.Palette.AWaypoint
                    .SetName(T.String (sprintf "WP%d" wpRank.[name]))
                    .SetArea(T.Integer 10)
                    .SetSpeed(T.Integer (getSpeedValue speed))
                    .SetPriority(T.Integer(int prio))
                    .SetXPos(T.Float pos.X)
                    .SetYPos(T.Float pos.Y)
                    .SetZPos(T.Float pos.Z)
                    .CreateMcuCommand()
            subst [res] |> ignore
            res
        
        let waypointInstances =
            Array2D.init 3 3 (fun prio speed ->
                let prio : WaypointPriority = enum prio
                let speed : TravelSpeed = enum speed
                // Skip some of the combinations. Too many waypoints leads to a crash in the mission editor.
                match speed, prio with
                | TravelSpeed.Normal, _
                | _, WaypointPriority.High -> []
                | _ ->
                    waypoints
                    |> List.map (fun wp -> createWaypoint prio speed (getPos wp) wp.Name.Value)
            )

        let vehicleLogic = newVehicles()
        subst vehicleLogic |> ignore
        let leader = getLeader vehicleLogic
        let vehicles =
            vehicleLogic
            |> List.choose (function :? HasEntity as veh -> Some veh | _ -> None)

        // Position and orientation of instantiated vehicles and logic
        let refLeaderPos = newVec3(0.0, 0.0, 0.0)
        vecCopy leader.Pos refLeaderPos
        let rotation = ori - leader.Ori.Y
        for veh in vehicleLogic do
            let pos2 =
                veh.Pos
                |> rotate refLeaderPos rotation
                |> fun v -> vecMinus v refLeaderPos
                |> translate pos
            vecCopy pos2 veh.Pos
            veh.Ori.X <- 0.0
            veh.Ori.Z <- 0.0
            veh.Ori.Y <- ori

        for veh in vehicles do
            veh.Name <- name

        // Russian models and country if necessary
        if swapToRussians then
            let russia = 101
            for veh in vehicles do
                veh.Script <- getRussianScriptEquivalent veh.Script
                veh.Model <- getRussianModelEquivalent veh.Model
                veh.Country <- russia

        waypointInstances
        |> Array2D.iter (List.iter (fun wp -> wp.Objects <- [leader.LinkTrId]))

        let mkOrderInput = mkInput subst name "Order"
        let stop = getCommandByName "Stop" vehicleLogic
        let stopSi = mkOrderInput stop
        let cont = getCommandByName "Continue" vehicleLogic
        let contSi = mkOrderInput cont
        let onRoad = getCommandByName "OnRoad" vehicleLogic
        let onRoadSi = mkOrderInput onRoad
        let attack = getCommandByName "Attack" vehicleLogic
        let attackSi = mkOrderInput attack
        let column = getCommandByName "Column" vehicleLogic
        let columnSi = mkOrderInput column
        let flareRed = getCommandByName "RedFlare" vehicleLogic
        let flareRedSi = mkOrderInput flareRed
        let flareGreen = getCommandByName "GreenFlare" vehicleLogic
        let flareGreenSi = mkOrderInput flareGreen

        let travelOrders =
            waypointInstances
            |> Array2D.mapi (fun prio speed wps ->
                wps
                |> List.map (fun wp ->
                    let prio =
                        match enum<WaypointPriority> prio with
                        | WaypointPriority.Low -> "F"
                        | WaypointPriority.Medium -> "R"
                        | WaypointPriority.High -> "H"
                        | x -> failwithf "Unexpected waypoint priority %A" x
                    let speed =
                        match enum<TravelSpeed> speed with
                        | TravelSpeed.Slow -> "S"
                        | TravelSpeed.Normal -> "N"
                        | TravelSpeed.Fast -> "F"
                        | x -> failwithf "Unexpected speed %A" x
                    let prefix = sprintf "%s%s" prio speed
                    let input = mkInput subst name prefix wp
                    subst [input] |> ignore
                    input.Targets <- [wp.Index]
                    input
                )
            )

        let wpCmds =
            [
                for i in 0..2 do
                    for j in 0..2 do
                        yield waypointInstances.[i,j]
            ]
            |> List.concat

        let travelOrders =
            [
                for prio in 0..2 do
                    for speed in 0..2 do
                        match enum speed, enum prio with
                        | TravelSpeed.Normal, _
                        | _, WaypointPriority.High ->
                            ()
                        | _ ->
                            yield travelOrders.[prio,speed]
            ]
            |> List.concat

        let all : McuBase list =
            vehicleLogic @@ wpCmds @@ travelOrders @@ [ stopSi ; contSi ; onRoadSi ; attackSi ; columnSi ; flareRedSi ; flareGreenSi ] @@ []

        { Leader = leader
          All = all
        }


type AntiTankGroup =
    { Leader : HasEntity
      All : McuBase list
    }
with
    interface IMcuGroup with
        member this.Content = this.All
        member this.SubGroups = []
        member this.LcStrings = []

    static member Create(subst : McuBase seq -> int -> int, pos : Vec3, ori : float, swapToGermans : bool, newCannons : unit -> McuBase list, getLeader : McuBase list -> HasEntity) =
        let cannonLogic = newCannons()
        subst cannonLogic |> ignore
        let leader = getLeader cannonLogic
        let cannons =
            cannonLogic
            |> List.choose (function :? HasEntity as cannon -> Some cannon | _ -> None)

        // Position and orientation of instantiated vehicles and logic
        let refLeaderPos = newVec3(0.0, 0.0, 0.0)
        vecCopy leader.Pos refLeaderPos
        let rotation = ori - leader.Ori.Y
        for cannon in cannonLogic do
            let pos2 =
                cannon.Pos
                |> rotate refLeaderPos rotation
                |> fun v -> vecMinus v refLeaderPos
                |> translate pos
            vecCopy pos2 cannon.Pos
            cannon.Ori.X <- 0.0
            cannon.Ori.Z <- 0.0
            cannon.Ori.Y <- ori

        // German models and country if necessary
        if swapToGermans then
            let germany = 201
            for veh in cannons do
                veh.Script <- getGermanArtilleryScriptEquivalent veh.Script
                veh.Model <- getGermanArtilleryModelEquivalent veh.Model
                veh.Country <- germany

        let all : McuBase list =
            cannonLogic @@ []

        { Leader = leader
          All = all
        }


type WaypointLabels =
    { Labels : T.MCU_Icon list
      LcStrings : (int * string) list
    }
with
    static member Create(subst : McuBase seq -> int -> int, waypoints : T.MCU_Waypoint list) =
        let labels =
            [
                for wp in waypoints do
                    let label =
                        T.Palette.Unnamed
                            .SetLCName(T.Integer 3)
                            .SetXPos(T.Float wp.XPos.Value)
                            .SetYPos(T.Float wp.YPos.Value)
                            .SetZPos(T.Float wp.ZPos.Value)
                    let lcText =
                        T.Palette.ASubtitle.CreateMcuCommand()
                    subst [lcText] |> ignore
                    let label =
                        label
                            .SetIndex(T.Integer lcText.Index)
                            .SetLCName(T.Integer lcText.SubtitleLC.Value.LCText)
                    yield label, (lcText.SubtitleLC.Value.LCText, wp.Name.Value)
            ]
        { Labels = labels |> List.map fst
          LcStrings = labels |> List.map snd
        }
    
let buildMission(outdir, basename) =
    printfn "Building %s" basename

    let parse(filename) =
        try
            T.GroupData(Parsing.Stream.FromFile(Path.Combine(T.ResolutionFolder, outdir, filename)))
        with
        | :? Parsing.ParseError as err ->
            let msg = Parsing.printParseError err
            eprintfn "Parse error: %s" (String.concat "\n" msg)
            raise err

    // The stores that are responsible for providing collision-free new identifiers.
    /// MCU Index store.
    let idStore = new IdStore()
    /// Localized string index store.
    let lcIdStore = new IdStore()
    lcIdStore.SetNextId(3) // 0, 1, 2 reserved for mission title, briefing and author

    /// Convenience function that creates id allocators and assigns fresh ids to a given sequence of MCUs.
    let subst (mcus : #McuBase seq) =
        let getNewId = idStore.GetIdMapper()
        let getNewLcId = lcIdStore.GetIdMapper()
        for mcu in mcus do
            substId getNewId mcu
            substLCId getNewLcId mcu
        getNewLcId

    // Static content
    let ground =
        try
            T.GroupData(Parsing.Stream.FromFile(Path.Combine(T.ResolutionFolder, outdir, "Static.Mission"))).CreateMcuList()
        with
        | :? Parsing.ParseError as err ->
            let msg = Parsing.printParseError err
            eprintfn "Parse error: %s" (String.concat "\n" msg)
            raise err
    let getLcId0 = subst ground
    let getLcId x =
        if x < 3 then x
        else getLcId0 x

    let groundLcStrings =
        Localization.transfer true getLcId (Path.Combine(T.ResolutionFolder, outdir, "Static.eng"))

    let waypoints =
        parse("Waypoints.Group").ListOfMCU_Waypoint
        |> List.sortBy (fun wp -> wp.Name.Value)

    // vehicles
    let vehicles =
        let vehicles =
            parse("Platoons.Group").ListOfVehicle
            |> List.sortBy (fun veh -> veh.Name.Value)
        [
            for refVehicle in vehicles do
                let swapToRussian, speed, createMcuList =
                    match refVehicle.Name.Value with
                    | StartsWith "Pz3" ->
                        false, 40, T.GerPz3.CreateMcuList
                    | StartsWith "T34" ->
                        true, 40, T.GerPz3.CreateMcuList
                    | StartsWith "Pz4" ->
                        false, 40, T.GerPz4.CreateMcuList
                    | StartsWith "T70" ->
                        true, 40, T.GerPz4.CreateMcuList
                    | StartsWith "1c" ->
                        false, 70, T.GerMechanizedInfantry.CreateMcuList
                    | StartsWith "ba10m" ->
                        true, 70, T.GerMechanizedInfantry.CreateMcuList
                    | StartsWith "szf"
                    | StartsWith "Szf" ->
                        false, 70, T.GerMobileArtillery.CreateMcuList
                    | StartsWith "bm13" ->
                        true, 70, T.GerMobileArtillery.CreateMcuList
                    | other ->
                        failwithf "Unsupported platoon type '%s'" other
                yield VehicleGroup.Create(
                    subst, refVehicle.Name.Value, getPos refVehicle, refVehicle.YOri.Value, swapToRussian, speed, createMcuList, getHasEntityByName "Leader", waypoints)
        ]

    // Defenses
    let defenses =
        [
            for refArty in parse("Defenses.Group").ListOfVehicle do
                let swapToGerman, createMcuList =
                    match refArty.Script.Value with
                    | EndsWith "zis3gun.txt" ->
                        false, T.RusAT.CreateMcuList
                    | EndsWith "pak40.txt" ->
                        true, T.RusAT.CreateMcuList
                    | EndsWith "52k.txt" ->
                        false, T.Rus52k.CreateMcuList
                    | EndsWith "flak37.txt" ->
                        true, T.Rus52k.CreateMcuList
                    | EndsWith "61k.txt" ->
                        false, T.Rus61k.CreateMcuList
                    | EndsWith "flak38.txt" ->
                        true, T.Rus61k.CreateMcuList
                    | other ->
                        failwithf "Unsupported artillery type '%s'" other
                yield AntiTankGroup.Create(
                    subst, getPos refArty, refArty.YOri.Value, swapToGerman, createMcuList, getHasEntityByName "Leader")
        ]

    let waypointLabels = WaypointLabels.Create(subst, waypoints)

    // Airfields
    let airfields = parse("Airfields.Group").CreateMcuList()
    subst airfields |> ignore

    // Write mission file
    using (File.CreateText(Path.Combine(T.ResolutionFolder, outdir, basename + ".Mission"))) (fun file ->
        file.WriteLine "# Mission File Version = 1.0;"
        file.WriteLine ""
        let options =
            let parser = T.Parser()
            try
                let s = Parsing.Stream.FromFile(Path.Combine(T.ResolutionFolder, outdir, "Static.Mission"))
                match s with
                | Parsing.ReLit "Options" s ->
                    parser.Parse_Options(s)
                | _ ->
                    Parsing.parseError("Expected 'Options'", s)
            with
            | :? Parsing.ParseError as err ->
                let msg = Parsing.printParseError err
                eprintfn "Parse error: %s" (String.concat "\n" msg)
                raise err
            |> fst
        file.Write(options.AsString())
        file.WriteLine ""
        let groundStr =
            ground
            |> McuUtil.moveEntitiesAfterOwners
            |> Seq.map (fun mcu -> mcu.AsString())
            |> String.concat "\n"
        file.Write(groundStr)
        for defense in defenses do
            file.Write(asString defense)
        for af in airfields do
            file.Write(af.AsString())
        for label in waypointLabels.Labels do
            file.Write(label.AsString())
        for platoon in vehicles do
            file.Write(asString platoon)
        file.WriteLine ""
        file.WriteLine "# end of file"
    )

    let createLcFile filename allLcStrings =
        use file = new StreamWriter(Path.Combine(T.ResolutionFolder, outdir, filename), false, System.Text.UnicodeEncoding(false, true))
        for (idx, s) in allLcStrings do
            file.WriteLine(sprintf "%d:%s" idx s)

    let allLcStrings = groundLcStrings @ waypointLabels.LcStrings

    for lang in [ "eng"; "ger"; "pol"; "rus" ] do
        createLcFile (basename + "." + lang) allLcStrings

//buildMission("VLuki", "GroundCommanderMini")
buildMission("Stalingrad", "GroundCommanderStalingrad")