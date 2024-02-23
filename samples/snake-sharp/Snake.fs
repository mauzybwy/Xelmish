module Snake

// *****************************************************************************
// * Import
// *****************************************************************************
open Xelmish.Model
open Xelmish.Viewables
open ImGuiNET
open ImGuiNET.XNA.FSharp
open Common

// *****************************************************************************
// * Model
// *****************************************************************************

type Model = { X: int; Y: int; W: int; H: int; MoveAmount: int }

let init () = { X = 0; Y = 0; W = 100; H = 100; MoveAmount = 1 }

// *****************************************************************************

let viewModel = {| MoveAmount = ref 1; W = ref 100; H = ref 100; TestInt = ref 0 |}

let syncViewModel (model: Model) =
    viewModel.MoveAmount.contents <- model.MoveAmount
    viewModel.W.contents <- model.W
    viewModel.H.contents <- model.H

// *****************************************************************************
// * Update
// *****************************************************************************

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

// *****************************************************************************

type Message =
    | Force of newModel: Model
    | MoveLeft
    | MoveRight
    | MoveUp
    | MoveDown
    | Resize of dir: int

let update message model =
    match message with
    | Force newModel -> newModel
    | MoveLeft -> moveHorizontal model (-1 * model.MoveAmount)
    | MoveRight -> moveHorizontal model model.MoveAmount
    | MoveUp -> moveVertical model (-1 * model.MoveAmount)
    | MoveDown -> moveVertical model model.MoveAmount
    | Resize dir ->
        { model with
            X = model.X - dir
            Y = model.Y - dir
            W = model.W + 2 * dir
            H = model.H + 2 * dir }

// *****************************************************************************
// * GUI View
// *****************************************************************************

let buildGui model dispatch =
    syncViewModel model

    let set newModel = dispatch (Force newModel)

    let setMoveAmount =
        fun _ -> set { model with MoveAmount = viewModel.MoveAmount.Value }

    let moveAmountSlider = fun () ->
        if ImGui.SliderInt("", viewModel.MoveAmount, 0, 10) then setMoveAmount()

    Gui.app
        [ Gui.window
              "UI State"
              [ Gui.text "These tools only affect the UI state"

                fun _ -> ImGui.SliderInt("Example Slider", viewModel.TestInt, -100, 100) |> ignore ]

          Gui.window
              "Game State"
              [ Gui.text "These tools affect the Game state"

                Gui.text "Move:"
                +++ Gui.button " < " (fun _ -> dispatch MoveLeft)
                +++ Gui.button " ^ " (fun _ -> dispatch MoveUp)
                +++ Gui.button " v " (fun _ -> dispatch MoveDown)
                +++ Gui.button " > " (fun _ -> dispatch MoveRight)

                Gui.text "Move By:" +++ moveAmountSlider ]

          Gui.statusBar
              "Status Bar"
              [ Gui.text $"Move By: {viewModel.MoveAmount.Value}"
                fun _ -> Gui.text $"{ImGui.GetIO().Framerate} FPS" () ] ]

// *****************************************************************************
// * View
// *****************************************************************************

let view model dispatch =
    [
        // Objects
        imgui (buildGui model dispatch)
        image "head" Colour.White (model.W, model.H) (model.X, model.Y)

        // I/O
        whilekeydown Keys.Up (fun _ -> dispatch (MoveUp))
        whilekeydown Keys.Down (fun _ -> dispatch (MoveDown))
        whilekeydown Keys.Left (fun _ -> dispatch (MoveLeft))
        whilekeydown Keys.Right (fun _ -> dispatch (MoveRight))

        whilekeydown Keys.OemPlus (fun _ -> dispatch (Resize 1))
        whilekeydown Keys.OemMinus (fun _ -> dispatch (Resize -1))

        onkeydown Keys.Escape exit
    ]
