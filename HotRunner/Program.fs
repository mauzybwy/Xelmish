open System
open System.IO
open System.Reflection

let mutable lastWriteTime = DateTime.MinValue
let watcher = new FileSystemWatcher()

let rec reloadDll (dllPath: string) =
    try
        // Unload the assembly if it's already loaded
        // let assemblyName = Assembly.GetExecutingAssembly().GetName().Name
        // let loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                             // |> Array.tryFind (fun asm -> asm.GetName().Name = assemblyName)
        let asm =
            Assembly.LoadFrom(dllPath)

        printfn "Assembly loaded %s" (asm.GetName().FullName)
        // let aaa = asm.EntryPoint.GetParameters ()
        // printfn "First param %s" (aaa[2].ToString ())
        asm.EntryPoint.Invoke(null, [| [|""|] |]) |> ignore
        ()

        // match loadedAssembly with
        // | Some asm ->
        //     let ass = AppDomain.CurrentDomain.Load(asm.GetName())
        //     // asm.EntryPoint.Invoke (null, null) |> ignore
        //     printfn "Assembly loaded %s" (asm.GetName().FullName)
        //     ()
        // | None ->
        //     printfn "Assembly not loaded"
        // |> ignore

    with
    | ex ->
        printfn "Error reloading DLL: %s" ex.InnerException.Message
        reloadDll dllPath

let watchDllChanges (dllPath: string) =
    let fname =  Path.GetFileName(dllPath)
    let dirname = Path.GetDirectoryName(dllPath)

    printfn "%s" fname
    printfn "%s" dirname

    // reloadDll()

    watcher.Path <- dirname
    watcher.Filter <- fname
    watcher.IncludeSubdirectories <- true
    // watcher.NotifyFilter <- NotifyFilters.Attributes |||
    //     NotifyFilters.CreationTime |||
    //     NotifyFilters.FileName |||
    //     NotifyFilters.LastAccess |||
    //     NotifyFilters.LastWrite |||
    //     NotifyFilters.Size |||
    //     NotifyFilters.Security;
    watcher.EnableRaisingEvents <- true
    // watcher.Changed.Add(fun _ -> printfn "CHANGED")
    watcher.Created.Add(fun (args: FileSystemEventArgs) ->
                        printfn "Loading %s" dllPath
                        reloadDll dllPath
                        // lastWriteTime <- lastWrite
                        // let lastWrite =  File.GetLastWriteTime(args.FullPath)
                        // if lastWrite <> lastWriteTime then

)



// Example usage
let dllPath = "../IkTest/bin/Debug/net7.0/SampleGame.dll"
watchDllChanges dllPath
reloadDll dllPath
//


// Keep the application running
Console.ReadLine() |> ignore
