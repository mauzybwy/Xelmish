﻿module StartScreen

open System
open System.IO
open Common
open Xelmish.Viewables
open Xelmish.Model

type Model = {
    highScore: int option
}

let init () =
    let score = 
        if File.Exists highScoreFile 
        then Some (int (File.ReadAllText highScoreFile))
        else None
    { highScore = score }

type Message = 
    | StartGame

let view model dispatch = 
    let centredText colour = text "PressStart2P" 24. colour (-0.5, 0.)
    let textMid = resWidth / 2
    [
        yield centredText Colour.White "HIGH  SCORE" (textMid, 10)
        let score = match model.highScore with Some score -> score | _ -> 0
        yield centredText Colour.White (sprintf "%04i" score) (textMid, 35)

        yield centredText Colour.OrangeRed "PLAY" (textMid, 90)
        yield centredText Colour.OrangeRed "SPACE  INVADERS" (textMid, 120)

        yield centredText Colour.Cyan "*  SCORE  ADVANCE  TABLE  *" (textMid, 180)

        for index, kind in [0, smallSize; 1, mediumSize; 2, largeSize] do
            let top = index * 50 + 240
            yield sprite kind.animations.[0] (kind.width, kind.height) (textMid - 170, top - 5) kind.colour
            yield centredText kind.colour (sprintf "=  %i  POINTS" kind.score) (textMid, top)

        yield centredText Colour.OrangeRed "(S)TART  GAME" (textMid, 450)
        yield centredText Colour.OrangeRed "(Q)UIT" (textMid, 480)

        yield onkeydown Keys.S (fun () -> dispatch StartGame)
        yield onkeydown Keys.Q exit
        yield onkeydown Keys.Escape exit

        yield centredText Colour.OrangeRed "ORIGINAL  GAME:  TAITO  1979" (textMid, resHeight - 60)
        yield centredText Colour.OrangeRed "REPROGRAMMED  GAME:  XELMISH  2019" (textMid, resHeight - 30)
    ]