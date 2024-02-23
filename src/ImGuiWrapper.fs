module Xelmish.ImGui.Wrappers

open ImGuiNET
open System.Numerics

type WindowConfig = {
    title: string option
    pos: (int * int) option
    size: (int * int) option
    flags: WindowFlags
} and WindowFlags = {
    noCollapse: bool
    noResize: bool
    noMove: bool
    noTitleBar: bool
    autoResize: bool
} with
    member internal __.AsImGuiWindowFlags =
        [
            if __.noCollapse then yield ImGuiWindowFlags.NoCollapse
            if __.noResize then yield ImGuiWindowFlags.NoResize
            if __.noMove then yield ImGuiWindowFlags.NoMove
            if __.noTitleBar then yield ImGuiWindowFlags.NoTitleBar
            if __.autoResize then yield ImGuiWindowFlags.AlwaysAutoResize
        ] |> List.reduce (|||)

let standardFlags = { noCollapse = true; noResize = true; noMove = false; noTitleBar = false; autoResize = true }

let window config children =
    fun model textures ->
        match config.pos with
            | None -> ()
            | Some (x, y) -> ImGui.SetNextWindowPos (new Vector2 (float32 x, float32 y))
        match config.size with
            | None -> ()
            | Some (w, h) -> ImGui.SetNextWindowSize (new Vector2 (float32 w, float32 h))
        let label = config.title |> Option.defaultValue ""
        ImGui.Begin (label, config.flags.AsImGuiWindowFlags) |> ignore
        let next = (model, children) ||> List.fold (fun last child -> child last textures)
        ImGui.End ()
        next

let text s =
    fun model _ ->
        ImGui.Text s; model

let button s update =
    fun model _ ->
        ImGui.Button s |> update model

let checkbox label startValue update =
    fun model _ ->
        let mutable state = startValue model
        ImGui.Checkbox (label, &state) |> ignore
        update model state

let textinput startValue maxLength update =
    fun model _ ->
        let mutable buffer = startValue model
        ImGui.InputText("", &buffer, uint32 maxLength) |> ignore
        update model buffer

let multilineinput startValue maxLength width height update =
    fun model _ ->
        let mutable buffer = startValue model
        ImGui.InputTextMultiline ("", &buffer, uint32 maxLength, new Vector2(float32 width, float32 height)) |> ignore
        update model buffer

let image assetKey width height =
    fun model textures ->
        let pointer = textures assetKey
        ImGui.Image (pointer, new Vector2(float32 width, float32 height))
        model

let row children =
    fun model textures ->
        let lasti = List.length children - 1
        ((0, model), children)
        ||> List.fold (fun (i, last) child ->
            let next = child last textures
            if i <> lasti then ImGui.SameLine ()
            i + 1, next)
        |> snd
