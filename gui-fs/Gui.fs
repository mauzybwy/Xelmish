namespace ImGuiNET.XNA.FSharp

open ImGuiNET
open System.Numerics

type ImGuiBuilder = unit -> unit

[<RequireQualifiedAccess>]
module Gui =
    let private runSeq seq = seq |> Seq.iter (fun i -> i())

    let app (builder:#seq<ImGuiBuilder>) = fun () ->
        builder |> Seq.iter (fun i -> i())

    let window (label: string) (builder:#seq<ImGuiBuilder>) = fun () ->
        if (ImGui.Begin(label)) then
            runSeq builder
            ImGui.End()

    let mainMenuBar (menus:#seq<ImGuiBuilder>) = fun () ->
        if ImGui.BeginMainMenuBar() then
            runSeq menus
            ImGui.EndMainMenuBar()

    let menu (label: string) (menuItems:#seq<ImGuiBuilder>) = fun () ->
        if ImGui.BeginMenu(label) then
            runSeq menuItems
            ImGui.EndMenu()

    let menuItemTogglable (label:string) (shortcut:string) (enabled:bool) (toggled:bool) onClick = fun () ->
        if ImGui.MenuItem(label, shortcut, toggled, enabled) then
            onClick()

    let menuItem (label:string) (shortcut:string) (enabled:bool) onClick =
        menuItemTogglable label shortcut enabled false onClick

    let button (label: string) onClick = fun () ->
        if (ImGui.Button(label)) then onClick()

    let buttonSmall (label: string) onClick = fun () ->
        if (ImGui.SmallButton(label)) then onClick()

    let checkbox (label: string) (var: bool ref) onClick = fun () ->
        if (ImGui.Checkbox(label, &var.contents)) then
            onClick var.Value

    let text (value: string) = fun () ->
        ImGui.Text(value)

    let textSpaced x (value: string) = fun () ->
        ImGui.SameLine(ImGui.GetCursorPosX() + (float32 x))
        ImGui.Text(value)

    let textFromLeft x (value: string) = fun () ->
        ImGui.SameLine(float32 x)
        ImGui.Text(value)

    let textFromRight x (value: string) = fun () ->
        let textSize = ImGui.CalcTextSize(value).X
        // let xPosition = ImGui.GetWindowContentRegionWidth() - textSize - (float32 x)
        let xPosition = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - textSize - (float32 x)
        ImGui.SameLine(xPosition)
        ImGui.Text(value)

    let coloredText (color:System.Drawing.Color) (value: string) = fun () ->
        ImGui.TextColored(Styles.colorToVec4 color, value)

    let alignText (f) = fun () ->
        ImGui.AlignTextToFramePadding()
        f()

    // let slider (value: float) = fun () ->
    //     ImGui.SliderFloat(value)

    let radioboxes (values: #seq<string * int>) (var: int ref) onClick onChange = fun () ->
        let oldValue = var.Value

        for label, value in values do
            if ImGui.RadioButton(label, &var.contents, value) then
                onClick var.Value

        if var.Value <> oldValue then
            onChange var.Value

    let sameLine (builder:#seq<ImGuiBuilder>) = fun () ->
        builder
        |> Seq.iteri (fun i item ->
            if (i > 0) then ImGui.SameLine()
            item()
        )

    let sameLine2 f1 width f2 = fun () ->
        f1()
        ImGui.SameLine(0f, float32 width)
        f2()

    let guard test (f:ImGuiBuilder) = if test then f else fun () -> ()

    let private statusBarFlags =
        ImGuiWindowFlags.NoMove |||
        ImGuiWindowFlags.NoResize |||
        ImGuiWindowFlags.NoCollapse |||
        ImGuiWindowFlags.NoSavedSettings |||
        ImGuiWindowFlags.NoTitleBar

    let statusBar (label: string) (statusBarItems:#seq<ImGuiBuilder>) = fun () ->
        let height = ImGui.GetTextLineHeight() + (ImGui.GetStyle().WindowPadding.Y * 2f)
        ImGui.SetNextWindowPos(Vector2(0f, ImGui.GetIO().DisplaySize.Y - height))
        ImGui.SetNextWindowSize(Vector2(ImGui.GetIO().DisplaySize.X, 0f))

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f)
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f)
        if (ImGui.Begin(label, statusBarFlags)) then
            sameLine(statusBarItems)()
            ImGui.End()
        ImGui.PopStyleVar()

[<AutoOpen>]
module Operators =
    let inline (++) (f1:ImGuiBuilder) (f2:ImGuiBuilder) =
        Gui.sameLine2 f1 1f f2

    let inline (+++) (f1:ImGuiBuilder) (f2:ImGuiBuilder) =
        Gui.sameLine2 f1 5f f2

    let inline (++++) (f1:ImGuiBuilder) (f2:ImGuiBuilder) =
        Gui.sameLine2 f1 10f f2
