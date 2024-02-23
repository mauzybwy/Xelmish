module Snake

open Xelmish.Model
open Xelmish.Viewables
open ImGuiNET
open ImGuiNET.XNA.FSharp
open Common

let PLAY_FIELD_WIDTH = 200
let PLAY_FIELD_HEIGHT = 200

type Model = { X: int; Y: int; W: int; H: int; }

let init () = { X = 0; Y = 0; W = 100; H = 100; }

let viewModel = {| MoveAmount = ref 1; W = ref 100; H = ref 100 |}

let syncViewModel (model: Model) =
    viewModel.W.contents <- model.W
    viewModel.H.contents <- model.H

type Message =
    | MoveVertical of dir: int
    | MoveHorizontal of dir: int
    | Resize of dir: int

let outOfBounds model =
    model.X < 0
    || model.X >= playField.W
    || model.Y < 0
    || model.Y > playField.H

let testOutOfBounds model proposed =
    if outOfBounds proposed then model else proposed

let moveVertical model dir =
    testOutOfBounds model { model with Y = model.Y + 2 * dir }

let moveHorizontal model dir =
    testOutOfBounds model { model with X = model.X + 2 * dir }

let update message model =
    match message with
    | MoveVertical dir -> moveVertical model dir
    | MoveHorizontal dir -> moveHorizontal model dir
    | Resize dir ->
        { model with
            X = model.X - dir
            Y = model.Y - dir
            W = model.W + 2 * dir
            H = model.H + 2 * dir }

let view model dispatch =
    syncViewModel model

    [
        image "head" Colour.White (model.W, model.H) (model.X, model.Y)

        imgui (Gui.app [
             Gui.window "SNAKEEEE" [
                 Gui.text "snake, baybeee"

                 //try (Gui.text $"{ImGui.GetIO().Framerate}") with | _ -> ()
                 Gui.button " < " (fun _ -> dispatch (MoveHorizontal viewModel.MoveAmount.Value))
                 ++ Gui.button " ^ " (fun _ -> dispatch (MoveHorizontal viewModel.MoveAmount.Value))
                 ++ Gui.button " v " (fun _ -> dispatch (MoveHorizontal viewModel.MoveAmount.Value))
                 ++ Gui.button " > " (fun _ -> dispatch (MoveHorizontal viewModel.MoveAmount.Value))
                 fun _ -> ImGui.SliderInt ("test", viewModel.MoveAmount, 0, 10) |> ignore
             ]

             Gui.statusBar "Status Bar" [
                Gui.text $"Move By: {viewModel.MoveAmount.Value}"
                fun _ -> Gui.text $"{ImGui.GetIO().Framerate} FPS" ()
            ]
         ])

        whilekeydown Keys.Up (fun _ -> dispatch (MoveVertical -1))
        whilekeydown Keys.Down (fun _ -> dispatch (MoveVertical 1))
        whilekeydown Keys.Left (fun _ -> dispatch (MoveHorizontal -1))
        whilekeydown Keys.Right (fun _ -> dispatch (MoveHorizontal 1))

        whilekeydown Keys.OemPlus (fun _ -> dispatch (Resize 1))
        whilekeydown Keys.OemMinus (fun _ -> dispatch (Resize -1))

        onkeydown Keys.Escape exit
    ]
