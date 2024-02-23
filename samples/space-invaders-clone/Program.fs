﻿open System.IO
open Elmish
open Xelmish.Model
open Xelmish.Viewables
open Common

type Model = 
    | Start of StartScreen.Model
    | Playing of Game.Model
    | GameOver of GameOver.Model

type Message = 
    | StartMessage of StartScreen.Message
    | PlayingMessage of Game.Message
    | GameOverMessage of GameOver.Message

let init () =
    let score = 
        if File.Exists highScoreFile 
        then int (File.ReadAllText highScoreFile)
        else 0
    Start (StartScreen.init score), Cmd.none

let update message model =
    match model, message with
    | Start _, StartMessage (StartScreen.StartGame highScore) -> 
        let model, command = Game.init highScore
        Playing model, command
    | Playing game, PlayingMessage msg -> 
        match msg with
        | Game.GameOver (score, highScore) -> GameOver (GameOver.init false score highScore), Cmd.none
        | Game.Victory (score, highScore) -> GameOver (GameOver.init true score highScore), Cmd.none
        | _ -> 
            let newModel, newCommand = Game.update msg game
            Playing newModel, Cmd.map PlayingMessage newCommand
    | GameOver _, GameOverMessage (GameOver.StartGame highScore) -> 
        let model, command = Game.init highScore
        Playing model, command
    | _ -> model, Cmd.none // invalid combination

let view model dispatch =
    let sampling = setPixelSampling () // this will ensure all our pixel graphics look sharp
    let screen = 
        match model with
        | Start startScreen -> StartScreen.view startScreen (StartMessage >> dispatch)
        | Playing game -> Game.view game (PlayingMessage >> dispatch)
        | GameOver gameOverScreen -> GameOver.view gameOverScreen (GameOverMessage >> dispatch)
    sampling::screen

[<EntryPoint>]
let main _ =
    let config: GameConfig = {
        clearColour = Some Colour.Black
        resolution = Windowed (resWidth, resHeight)
        assetsToLoad = [ 
            FileTexture ("sprites", "./content/sprites.png")
            PipelineFont ("PressStart2P", "./content/PressStart2P")
            FileSound ("startgame", "./content/siclone_menu_enter.wav")
            FileSound ("shoot", "./content/siclone_shoot.wav")
            FileSound ("explosion", "./content/siclone_explosion.wav")
            FileSound ("explosion-small", "./content/siclone_explosion_small.wav") 
            FileSound ("beep", "./content/siclone_menu.wav")
            FileSound ("gameover", "./content/siclone_death.wav")
            FileSound ("victory", "./content/siclone_saucer.wav") ]
        mouseVisible = false
    }

    Program.mkProgram init update view
    |> Xelmish.Program.runGameLoop config

    0
