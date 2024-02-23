open Elmish
open Xelmish.Model // required for config types used when using program.run
open Xelmish.Viewables // required to get access to helpers like 'colour'
open System.IO;

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
    || model.x >= PLAY_FIELD_WIDTH
    || model.y < 0
    || model.y > PLAY_FIELD_HEIGHT

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
    // we resize by not only changing dims but also pos, so the shape stays in the same place
    | Resize dir -> { x = model.x - dir; y = model.y - dir; w = model.w + 2 * dir; h = model.h + 2 * dir }

// The view method below is the primary 'Xelmish' part of Xelmish - all of the above is pure Elmish and platform independent.
// A view method returns two things: drawables and updatables (instances of OnDraw and OnUpdate, from Xelmish.Model): these
// package functions that are run during the Update and Draw methods of the core game loop. 

let view model dispatch =
    [
        // this is the only 'drawable' of our sample, nice and simple. it uses no loaded assets
        // colour Colour.Aqua (model.w, model.h) (model.x, model.y)

        image "test" Colour.White (model.w, model.h) (model.x, model.y)
        
        // a note for OnDraw methods like the above. It is technically possible to use dispatch within an OnDraw
        // method, obviously, but in almost all cases you shouldn't do this. You want the draw methods to run as fast as 
        // possible as they are rendering to the screen, and completely rebuilding the model mid stream is a bad idea. Keep
        // use of dispatch to updates, like the calls below.

        // various event helpers are in the Viewables module. whilekeydown will trigger every update
        whilekeydown Keys.Up (fun _ -> dispatch (MoveVertical -1))
        whilekeydown Keys.Down (fun _ -> dispatch (MoveVertical 1))
        whilekeydown Keys.Left (fun _ -> dispatch (MoveHorizontal -1))
        whilekeydown Keys.Right (fun _ -> dispatch (MoveHorizontal 1))

        whilekeydown Keys.OemPlus (fun _ -> dispatch (Resize 1))
        whilekeydown Keys.OemMinus (fun _ -> dispatch (Resize -1))

        // as a nice simple way to exit the app, we use onkeydown (triggers just the first press)
        // with the exit helper method (which has the signature fun _ -> throw exit exception)
        // NOTE: when referencing Xelmish from nuget or a dll, this call will cause the debugger to
        // halt. You can safely continue when it does so, as it will be caught by Xelmish.
        onkeydown Keys.Escape exit
    ]

[<EntryPoint>]
let main _ =
    // in this simple example, we can use the Elmish mkSimple and
    // the Xelmish runSimple, which preconfigures a windowed game for you
    Program.mkSimple init update view
    |> Xelmish.Program.runSimpleGameLoop [ AsepriteTexture ("test", Path.Combine("Content", "Art", "test.aseprite")) ] (PLAY_FIELD_WIDTH, PLAY_FIELD_HEIGHT) Colour.Black
    0
    
