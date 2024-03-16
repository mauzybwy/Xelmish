module Player

#r "nuget:Elmish"
#r "nuget:ImGUI.Net"
#r "nuget:MonoGame.Framework.DesktopGL"
#r "nuget:MonoGame.Reload"

#I "../Xelmish/bin/Debug/net7.0"
#r "Xelmish.dll"
#r "ImGuiNET.XNA.FSharp.dll"

// *****************************************************************************
// * Import
// *****************************************************************************
open Xelmish.Model
open Xelmish.Viewables
open ImGuiNET
open ImGuiNET.XNA.FSharp
//open Common
open Microsoft.Xna.Framework.Input
open System.Numerics

// *****************************************************************************
// * Types
// *****************************************************************************

type Position = { X: int; Y: int; }
type Segment = { Head: Vector2; Tail: Vector2; _Length: float32 }

// *****************************************************************************
// * Model
// *****************************************************************************

type Model = { Pos: Position; Body: Segment }

let init (length: float32) =

    { Pos = { X = 1; Y = 1 }
      Body =
        { _Length = length
          Head = Vector2(0f, 0f)
          Tail = Vector2(0f, length) } }

// *****************************************************************************

let viewModel = {|  |}

let syncViewModel (model: Model) =
    ()

// *****************************************************************************
// * Update Helpers
// *****************************************************************************

let follow model x y =
    let target = Vector2(x, y)
    let direction = target - model.Body.Tail
    // printfn $"{direction.X} {direction.Y}"
    let normalized = Vector2.Normalize(direction)
    let newTail = -1f * model.Body._Length * normalized + target
    let newHead = Vector2(model.Body.Head.X + direction.X, model.Body.Head.Y + direction.Y)

    { model with Body.Head = newHead; Body.Tail = newTail }

// *****************************************************************************
// * Update
// *****************************************************************************

type Message =
    | Force of newModel: Model
    | Follow of int * int

let update message model =
    match message with
    | Force newModel -> newModel
    | Follow (x, y) -> follow model (float32 x) (float32 y)

// *****************************************************************************
// * GUI View
// *****************************************************************************

let buildGui model dispatch =
    syncViewModel model

    let set newModel = dispatch (Force newModel)

    Gui.app
        [ Gui.window
              "Game State"
              [ Gui.text "These tools affect the Game state" ]

          Gui.statusBar
              "Status Bar"
              [ fun _ -> Gui.text $"{Mouse.GetState().X}" ()
                fun _ -> Gui.text $"{Mouse.GetState().Y}" ()
                fun _ -> Gui.text $"{ImGui.GetIO().Framerate} FPS" () ] ]

// *****************************************************************************
// * View
// *****************************************************************************

let view model dispatch =
    [
        // Objects
        //imgui (buildGui model dispatch)
        //image "player" Colour.White (100, 100) (model.Pos.X , model.Pos.Y)
        colour Colour.Pink (5,5) (int model.Body.Head.X, int model.Body.Head.Y)
        colour Colour.Yellow (5,5) (int model.Body.Tail.X, int model.Body.Tail.Y)

        // I/O
        onfollowmouse (fun (x: int, y: int) -> dispatch (Follow(x, y)))

        onkeydown Keys.Escape exit
    ]
