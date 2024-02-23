module Snake

open Elmish
open Xelmish.Model
open Xelmish.Viewables
open ImGuiNET.XNA.FSharp
open Common

let PLAY_FIELD_WIDTH = 200
let PLAY_FIELD_HEIGHT = 200

type Model = { x: int; y: int; w: int; h: int; }

let init () = { x = 0; y = 0; w = 100; h = 100 }

type Message =
    | MoveVertical of dir: int
    | MoveHorizontal of dir: int
    | Resize of dir: int

let outOfBounds model =
    model.x < 0
    || model.x >= playField.W
    || model.y < 0
    || model.y > playField.H

let testOutOfBounds model proposed =
    if outOfBounds proposed then model else proposed

let moveVertical model dir =
    testOutOfBounds model { model with y = model.y + 2 * dir }

let moveHorizontal model dir =
    testOutOfBounds model { model with x = model.x + 2 * dir }

let update message model =
    match message with
    | MoveVertical dir -> moveVertical model dir
    | MoveHorizontal dir -> moveHorizontal model dir
    | Resize dir -> { x = model.x - dir; y = model.y - dir; w = model.w + 2 * dir; h = model.h + 2 * dir }

let view model dispatch =
    [
        image "head" Colour.White (model.w, model.h) (model.x, model.y)

        whilekeydown Keys.Up (fun _ -> dispatch (MoveVertical -1))
        whilekeydown Keys.Down (fun _ -> dispatch (MoveVertical 1))
        whilekeydown Keys.Left (fun _ -> dispatch (MoveHorizontal -1))
        whilekeydown Keys.Right (fun _ -> dispatch (MoveHorizontal 1))

        whilekeydown Keys.OemPlus (fun _ -> dispatch (Resize 1))
        whilekeydown Keys.OemMinus (fun _ -> dispatch (Resize -1))

        onkeydown Keys.Escape exit
    ]
