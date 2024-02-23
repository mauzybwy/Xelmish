module Xelmish.ImGui.GameLoop

open Xelmish.GameLoop
open Xelmish.Model
open ImGuiNET
open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Input

type Vector2 = System.Numerics.Vector2

type BufferSet<'TBuffer> = {
    data: byte []
    buffer: 'TBuffer
    size: int
}

/// Largely a F# port of https://github.com/mellinoe/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram.XNA/ImGuiRenderer.cs
/// Credit to mellinoe@gmail.com
type ImGuiGameLoop (config: GameConfig) as this =
    inherit GameLoop (config)

    let mutable lastUIModel = Unchecked.defaultof<'TUIModel>

    let mutable loadedTextures = Map.empty
    let mutable gameCoreTextures = Map.empty
    let mutable lastTextureId = 0

    let mutable font = Unchecked.defaultof<ImFontPtr>
    let mutable lastScrollWheel = 0

    let vertSize = sizeof<ImDrawVert>
    let vertDeclaration =
        new VertexDeclaration (
            vertSize,
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        )

    let mutable vertexes = { data = [||]; buffer = Unchecked.defaultof<VertexBuffer>; size = 0 }
    let mutable indices = { data = [||]; buffer = Unchecked.defaultof<IndexBuffer>; size = 0 }

    let rasteriserState =
        new RasterizerState
            (CullMode = CullMode.None,
            DepthBias = 0.f,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0.f)

    do
        ImGui.CreateContext ()
        |> ImGui.SetCurrentContext

    let keys =
        //let io = ImGui.GetIO ()
        let keyMap = [
            ImGuiKey.Tab, Keys.Tab; ImGuiKey.LeftArrow, Keys.Left; ImGuiKey.RightArrow, Keys.Right
            ImGuiKey.UpArrow, Keys.Up; ImGuiKey.DownArrow, Keys.Down; ImGuiKey.PageUp, Keys.PageUp
            ImGuiKey.PageDown, Keys.PageDown; ImGuiKey.Home, Keys.Home; ImGuiKey.End, Keys.End
            ImGuiKey.Delete, Keys.Delete; ImGuiKey.Backspace, Keys.Back; ImGuiKey.Enter, Keys.Enter
            ImGuiKey.Escape, Keys.Escape; ImGuiKey.A, Keys.A; ImGuiKey.C, Keys.C
            ImGuiKey.V, Keys.V; ImGuiKey.X, Keys.X; ImGuiKey.Y, Keys.Y; ImGuiKey.Z, Keys.Z ]
        keyMap
        |> List.map (fun (dest, source) ->
            //io.KeyMap.[int dest] <- int source
            int source, dest)

    do
        let io = ImGui.GetIO ()
        let handler = fun _ (evt: TextInputEventArgs) ->
            if evt.Character <> '\t' then io.AddInputCharacter (uint32 evt.Character)
        this.Window.TextInput.AddHandler (new EventHandler<TextInputEventArgs> (handler))
        io.Fonts.AddFontDefault () |> ignore

    let bindTexture texture =
        let id = new IntPtr (lastTextureId)
        lastTextureId <- lastTextureId + 1
        loadedTextures <- Map.add id texture loadedTextures
        id

    let rebuildFontAtlas () =
        let io = ImGui.GetIO ()
        font <- io.Fonts.AddFontFromFileTTF ("Karla-Regular.ttf", 18.f)
        //let (pixelData, width, height, bytesPerPixel) = io.Fonts.GetTexDataAsRGBA32 ()
        let mutable pixelData = NativePtr.stackalloc<byte>(0)
        let mutable width = 0
        let mutable height = 0
        let mutable bytesPerPixel = 0
        io.Fonts.GetTexDataAsRGBA32(&pixelData, &width, &height, &bytesPerPixel)

        let pixelData = NativePtr.toNativeInt pixelData
        let pixels = Array.create<byte> (width * height * bytesPerPixel) 0uy
        Marshal.Copy(pixelData, pixels, 0, pixels.Length)

        let tex2d = new Texture2D(this.GraphicsDevice, width, height, false, SurfaceFormat.Color)
        tex2d.SetData(pixels)

        let newId = bindTexture tex2d
        io.Fonts.SetTexID newId
        io.Fonts.ClearTexData ()


    let updateInput (presentParams: PresentationParameters) =
        let io = ImGui.GetIO ()
        let mouse, keyboard = Mouse.GetState (), Keyboard.GetState ()
        //keys |> List.iter (fun key -> io.KeysDown.[key] <- keyboard.IsKeyDown (enum<Keys>(key)))
        // keys |> List.iter (fun key ->
        //                    let isDown = keyboard.IsKeyDown (enum<Keys>(key))
        //                    io.KeysData.[key].Down <- Convert.ToByte(isDown))
        keys |> List.iter (fun (key, imguikey) -> io.AddKeyEvent(imguikey, keyboard.IsKeyDown (enum<Keys>(key))))

        io.KeyShift <- keyboard.IsKeyDown Keys.LeftShift || keyboard.IsKeyDown Keys.RightShift
        io.KeyCtrl <- keyboard.IsKeyDown Keys.LeftControl || keyboard.IsKeyDown Keys.RightControl
        io.KeyAlt <- keyboard.IsKeyDown Keys.LeftAlt || keyboard.IsKeyDown Keys.RightAlt
        io.KeySuper <- keyboard.IsKeyDown Keys.LeftWindows || keyboard.IsKeyDown Keys.RightWindows

        io.DisplaySize <- new Vector2 (float32 presentParams.BackBufferWidth, float32 presentParams.BackBufferHeight)
        io.DisplayFramebufferScale <- new Vector2 (1.f, 1.f)

        io.MousePos <- new Vector2 (float32 mouse.X, float32 mouse.Y)
        [mouse.LeftButton;mouse.RightButton;mouse.MiddleButton]
        |> List.iteri (fun i b -> io.MouseDown.[i] <- b = ButtonState.Pressed)

        let scrollDelta = mouse.ScrollWheelValue - lastScrollWheel
        io.MouseWheel <- if scrollDelta > 0 then 1.f elif scrollDelta < 0 then -1.f else 0.f
        lastScrollWheel <- scrollDelta

    let updateBuffers (drawData: ImDrawDataPtr) =
        if drawData.TotalVtxCount > vertexes.size then
            if vertexes.buffer <> null then vertexes.buffer.Dispose ()
            let size = int (float drawData.TotalVtxCount * 1.5)
            vertexes <- {
                size = size
                buffer = new VertexBuffer(this.GraphicsDevice, vertDeclaration, size, BufferUsage.None)
                data = Array.zeroCreate(size * vertSize)
            }

        if drawData.TotalIdxCount > indices.size then
            if indices.buffer <> null then indices.buffer.Dispose ()
            let size = int (float drawData.TotalIdxCount * 1.5)
            indices <- {
                size = size
                buffer = new IndexBuffer(this.GraphicsDevice, IndexElementSize.SixteenBits, size, BufferUsage.None)
                data = Array.zeroCreate(size * sizeof<uint16>)
            }

        let mutable vtxOffset, idxOffset = 0, 0
        for n = 0 to drawData.CmdListsCount - 1 do
            let cmdList = drawData.CmdLists.[n]

            let vtxDstPtr = NativePtr.toVoidPtr &&vertexes.data.[vtxOffset * vertSize]
            Buffer.MemoryCopy (cmdList.VtxBuffer.Data.ToPointer (), vtxDstPtr, int64 vertexes.data.Length, int64 cmdList.VtxBuffer.Size * int64 vertSize)
            let idxDstPtr = NativePtr.toVoidPtr &&indices.data.[idxOffset * sizeof<uint16>]
            Buffer.MemoryCopy (cmdList.IdxBuffer.Data.ToPointer (), idxDstPtr, int64 indices.data.Length, int64 cmdList.IdxBuffer.Size * int64 sizeof<uint16>)

            vtxOffset <- vtxOffset + cmdList.VtxBuffer.Size
            idxOffset <- idxOffset + cmdList.IdxBuffer.Size

        vertexes.buffer.SetData (vertexes.data, 0, drawData.TotalVtxCount * vertSize)
        indices.buffer.SetData (indices.data, 0, drawData.TotalIdxCount * sizeof<uint16>)

    let updateEffect texture2d =
        let io = ImGui.GetIO ()
        let projection =
            Matrix.CreateOrthographicOffCenter (0.5f, io.DisplaySize.X + 0.5f, io.DisplaySize.Y + 0.5f, 0.5f, -1.f, 1.f)
        new BasicEffect (this.GraphicsDevice,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = projection,
                TextureEnabled = true,
                Texture = texture2d,
                VertexColorEnabled = true)

    let renderCommandLists (drawData: ImDrawDataPtr) =
        this.GraphicsDevice.SetVertexBuffer vertexes.buffer
        this.GraphicsDevice.Indices <- indices.buffer

        let mutable vtxOffset, idxOffset = 0, 0

        for n = 0 to drawData.CmdListsCount - 1 do
            let cmdList = drawData.CmdLists.[n]

            for cmdi = 0 to cmdList.CmdBuffer.Size - 1 do
                let drawCmd = cmdList.CmdBuffer.[cmdi]

                match Map.tryFind drawCmd.TextureId loadedTextures with
                | None -> failwith (sprintf "Could not find a texture with id '%A', please check your bindings" drawCmd.TextureId)
                | Some texture2d ->

                    this.GraphicsDevice.ScissorRectangle <- new Rectangle(
                            int drawCmd.ClipRect.X,
                            int drawCmd.ClipRect.Y,
                            int (drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                            int (drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                        )

                    let effect = updateEffect texture2d
                    effect.CurrentTechnique.Passes |> Seq.iter (fun pass ->
                        pass.Apply ()
                        this.GraphicsDevice.DrawIndexedPrimitives (
                            PrimitiveType.TriangleList,
                            vtxOffset,
                            0,
                            cmdList.VtxBuffer.Size,
                            idxOffset,
                            int drawCmd.ElemCount / 3))

                idxOffset <- idxOffset + int drawCmd.ElemCount

            vtxOffset <- vtxOffset + cmdList.VtxBuffer.Size

    let renderDrawData (presentParams: PresentationParameters) (drawData: ImDrawDataPtr) =
        let io = ImGui.GetIO ()
        let lastViewport = this.GraphicsDevice.Viewport
        let lastScissorsBox = this.GraphicsDevice.ScissorRectangle

        this.GraphicsDevice.BlendFactor <- Color.White
        this.GraphicsDevice.BlendState <- BlendState.NonPremultiplied
        this.GraphicsDevice.RasterizerState <- rasteriserState
        this.GraphicsDevice.DepthStencilState <- DepthStencilState.DepthRead

        drawData.ScaleClipRects io.DisplayFramebufferScale

        this.GraphicsDevice.Viewport <- new Viewport(0, 0, presentParams.BackBufferWidth, presentParams.BackBufferHeight)

        if drawData.TotalVtxCount > 0 then updateBuffers drawData
        renderCommandLists drawData

        this.GraphicsDevice.Viewport <- lastViewport
        this.GraphicsDevice.ScissorRectangle <- lastScissorsBox

    // let getTexure assetKey =
    //     match Map.tryFind assetKey gameCoreTextures with
    //     | Some pointer -> pointer
    //     | None ->
    //         let texture = this.Texture assetKey
    //         let pointer = bindTexture texture
    //         gameCoreTextures <- Map.add assetKey pointer gameCoreTextures
    //         pointer

    member internal __.LastUIModel
        with get () = lastUIModel

    override __.Initialize () =
        rebuildFontAtlas ()
        base.Initialize ()

    override __.Draw (gameTime) =
        base.Draw (gameTime)

        // match base.Model with
        // | Some model ->

        // let io = ImGui.GetIO ()
        // io.DeltaTime <- float32 gameTime.ElapsedGameTime.TotalSeconds
        // let presentParams = this.GraphicsDevice.PresentationParameters
        // updateInput presentParams
        // ImGui.NewFrame ()

        // ImGui.PushFont font

        // lastUIModel <- (getUI model ||> List.fold (fun last element -> element last getTexure))

        // ImGui.Render ()
        // renderDrawData presentParams (ImGui.GetDrawData ())

        // ()

        // | _ -> ()
