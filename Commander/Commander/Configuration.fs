﻿module Configuration

open System.IO

type Config = FSharp.Data.JsonProvider<"SampleConfig.json", SampleIsList=true>

let values =
    if File.Exists("Configuration.json") then
        Config.Load("Configuration.json")
    else
        Config.Load("SampleConfig.json")
