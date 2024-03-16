#r "nuget: Elmish"
#r "nuget: ImGUI.Net"
#r "nuget: MonoGame.Framework.DesktopGL"
#r "nuget: MonoGame.Reload"

#I "../Xelmish/bin/Debug/net7.0"
#r "Xelmish.dll"
#r "ImGuiNET.XNA.FSharp.dll"

#load "Common.fsx"
open Common

#load "Player.fsx"
open Player

// module Program

open Elmish
open Xelmish.Model
open System.IO;
// open Common;

type Model = {
    player: Player.Model
}

let init () = { player = Player.init (100f) }

type Message =
    | PlayerMessage of Player.Message

let update message model =
    match message with
    | PlayerMessage message ->
        { model with player = Player.update message model.player }

let view model dispatch =
    printfn $"HEYYYYYY"
    [
       yield! Player.view model.player (PlayerMessage >> dispatch)
    ]

// [<EntryPoint>]
let main _ =
    let assetsToLoad =
        [ AsepriteTexture ("player", Path.Combine("Content", "Art", "player.aseprite")) ]
    
    Program.mkSimple init update view
    |> Xelmish.Program.runSimpleGameLoop assetsToLoad (playField.W, playField.H) Colour.Black
    0

main()
