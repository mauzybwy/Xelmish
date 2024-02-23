open Elmish
open Xelmish.Model
open System.IO;
open Common;

type Model = {
    snake: Snake.Model
}

let init () = { snake = Snake.init () }

type Message =
    | SnakeMessage of Snake.Message

let update message model =
    match message with
    | SnakeMessage message ->
        { model with snake = Snake.update message model.snake }

let view model dispatch =
    [
       yield! Snake.view model.snake (SnakeMessage >> dispatch)
    ]

[<EntryPoint>]
let main _ =
    let assetsToLoad =
        [ AsepriteTexture ("head", Path.Combine("Content", "Art", "head.aseprite")) ]
    
    Program.mkSimple init update view
    |> Xelmish.Program.runSimpleGameLoop assetsToLoad (playField.W, playField.H) Colour.Black
    0
    
