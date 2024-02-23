module internal Xelmish.GameLoop

open System.IO
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Audio
open Microsoft.Xna.Framework.Media
open Model
open MonoGame.Aseprite;
open MonoGame.Aseprite.Content.Processors;
open ImGuiNET;
open ImGuiNET.XNA
open ImGuiNET.XNA.FSharp;

/// GameLoop is an inherited implementation of XNA's Game class
type GameLoop (config: GameConfig) as this = 
    inherit Game ()

    let graphics = new GraphicsDeviceManager (this)
    
    // these are set during LoadContent
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable assets = Unchecked.defaultof<_>
    let mutable imguiRenderer = Unchecked.defaultof<ImGuiRenderer>
    
    // this is set and updated every Update (60 times a second)
    let mutable inputs = {
        keyboardState = Unchecked.defaultof<_>
        lastKeyboardState = Unchecked.defaultof<_>
        mouseState = Unchecked.defaultof<_>
        lastMouseState = Unchecked.defaultof<_>
        gameTime = Unchecked.defaultof<_>
    }

    // these two collections are set by the Elmish setState call
    let mutable updatable: (Inputs -> Unit) list = []
    let mutable drawable: (LoadedAssets -> Inputs -> SpriteBatch -> Unit) list = []
        
    /// Used by Xelmish with the Elmish setState. 
    /// Viewables from the Elmish components are accepted 
    /// and assigned internally here for update and drawing
    member __.View
        with set value = 
            let rec splitter updatableAcc drawableAcc =
                function
                | [] -> 
                    updatable <- updatableAcc
                    drawable <- drawableAcc
                | (OnUpdate f)::rest -> splitter (f::updatableAcc) drawableAcc rest
                | (OnDraw f)::rest -> splitter updatableAcc (f::drawableAcc) rest
            // we split the viewables by their DU type to be more efficient during draw/update
            splitter [] [] (List.rev value)

    override __.Initialize () = 
        // Set up overall run settings here, like resolution, screen type and fps

        imguiRenderer <- new ImGuiRenderer(this)
        imguiRenderer.RebuildFontAtlas()

        let setRes w h = 
            graphics.PreferredBackBufferWidth <- w
            graphics.PreferredBackBufferHeight <- h
            graphics.GraphicsDevice.SetRenderTarget(new RenderTarget2D(graphics.GraphicsDevice, w, h))

        match config.resolution with
        | Windowed (w, h) -> 
            this.Window.AllowUserResizing <- true 
            setRes w h
        | FullScreen (w, h) -> 
            graphics.IsFullScreen <- true
            setRes w h
        | Borderless (w, h) -> 
            this.Window.IsBorderless <- true 
            setRes w h
       

        this.IsMouseVisible <- config.mouseVisible
        
        // This makes draw run at monitor fps, rather than 60fps
        graphics.SynchronizeWithVerticalRetrace <- true 

        graphics.ApplyChanges()

        base.Initialize() 

    override __.LoadContent () = 
        spriteBatch <- new SpriteBatch (graphics.GraphicsDevice)

        // Assets are loaded into a reference record type (LoadedAssets) here.
        // Both file based and content pipeline based resources are accepted. 
        let loadIntoAssets assets loadable =
            match loadable with
            | FileTexture (key, path) -> 
                use stream = File.OpenRead path
                let texture = Texture2D.FromStream (this.GraphicsDevice, stream)
                { assets with textures = Map.add key texture assets.textures }
            | PipelineTexture (key, path) ->
                let texture = this.Content.Load<Texture2D> path
                { assets with textures = Map.add key texture assets.textures }
            | AsepriteTexture (key, path) ->
                let aseFile = AsepriteFile.Load(path)
                let aseprite = SpriteProcessor.Process(this.GraphicsDevice, aseFile, 0)
                { assets with textures = Map.add key aseprite.TextureRegion.Texture assets.textures }
            | PipelineFont (key, path) -> 
                let font = this.Content.Load<SpriteFont> path
                { assets with fonts = Map.add key font assets.fonts }
            | FileSound (key, path) -> 
                use stream = File.OpenRead path
                let sound = SoundEffect.FromStream stream
                { assets with sounds = Map.add key sound assets.sounds }
            | PipelineSound (key, path) ->
                let sound = this.Content.Load<SoundEffect> path
                { assets with sounds = Map.add key sound assets.sounds }
            | FileMusic (key, path) -> 
                let uri = new System.Uri (path, System.UriKind.RelativeOrAbsolute)
                let music = Song.FromUri (key, uri)
                { assets with music = Map.add key music assets.music }
            | PipelineMusic (key, path) ->
                let music = this.Content.Load<Song> path
                { assets with music = Map.add key music assets.music }

        let loadedAssets = 
            { whiteTexture = new Texture2D (this.GraphicsDevice, 1, 1)
              textures = Map.empty 
              fonts = Map.empty 
              sounds = Map.empty 
              music = Map.empty }
        // for rendering pure colour, rather than requiring the user load a colour texture
        // we create one, a single white pixel, that can be resized and coloured as needed.
        loadedAssets.whiteTexture.SetData<Color> [| Color.White |]
        assets <- List.fold loadIntoAssets loadedAssets config.assetsToLoad

    override __.Update gameTime =
        // update inputs. last keyboard and mouse state are preserved so changes can be detected
        inputs <- 
            {   lastKeyboardState = inputs.keyboardState
                keyboardState = Keyboard.GetState ()
                lastMouseState = inputs.mouseState
                mouseState = Mouse.GetState ()
                gameTime = gameTime }

        try
            for updateFunc in updatable do updateFunc inputs
        with
            // quit game is a custom exception used by elmish 
            // components to tell the game to quit gracefully
            | :? QuitGame -> __.Exit()

    override __.Draw gameTime =
        // Clear
        Option.iter this.GraphicsDevice.Clear config.clearColour

        // ImGui Test
        // imguiRenderer.BeforeLayout(gameTime);
        // let gui = Gui.app [
        //     Gui.window "Demo" [
        //         Gui.text "hello, world"
        //     ]
        // ]
        // gui()

        // let gui2 = Gui.app [
        //     Gui.window "Hmmmmmm" [
        //         Gui.text "hello, world"
        //     ]
        // ]
        // gui2()
        // imguiRenderer.AfterLayout();

        // by default, all sprites are drawing on .End() in a batch
        // immediate changes this so they are drawn as called, which allows us to
        // change the sampler state (e.g. for pixel graphics vs text) between different sprite calls
        spriteBatch.Begin (sortMode = SpriteSortMode.Immediate,
                            samplerState = SamplerState.PointClamp)

        imguiRenderer.BeforeLayout(gameTime);

        //  Gui.window "Hmmmmmm" [
        //     Gui.text "hello, world"
        //     Gui.text $"{ImGui.GetIO().Framerate} FPS"
        // ]()

        for drawFunc in drawable do drawFunc assets inputs spriteBatch

        imguiRenderer.AfterLayout();

        spriteBatch.End ()


    // override __.Draw gameTime value =
        // ()
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
