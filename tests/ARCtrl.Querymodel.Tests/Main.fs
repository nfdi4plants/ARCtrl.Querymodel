module arcIO.NET.Tests

open Fable.Pyxpecto
open Fable.Core


#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
module TSUtils = 

    open Fable.Pyxpecto
    open Fable.Core
    open Fable.Core.JS


    [<Import("it", from = "vitest")>]
    let it(name: string, test: unit -> unit) = jsNative

    [<Import("it", from = "vitest")>]
    let itAsync(name: string, test: unit -> Promise<unit>) = jsNative

    [<Import("describe", from = "vitest")>]
    let describe(name: string, testSuit: unit -> unit) = jsNative 


    // module Promise = 
    //     [<Emit("void $0")>]
    //     let start (pr: JS.Promise<'T>): unit = jsNative

    //     [<Emit("void ($1.then($0))")>]
    //     let iter (a: 'T -> unit) (pr: JS.Promise<'T>): unit = jsNative

    //     [<Emit("$1.then($0)")>]
    //     let map (a: 'T1 -> 'T2) (pr: JS.Promise<'T1>): JS.Promise<'T2> = jsNative
    //     let runTests = runTests   
#endif


let all = testSequenced <| testList "All" [
        TestARC.Tests.main     
        FragmentSelector.Tests.main
        ProcessCore.Tests.main
    ]

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open TSUtils

describe("index", fun () -> 
    itAsync ("add", fun () ->
        Pyxpecto.runTestsAsync [| ConfigArg.DoNotExitWithCode|] all
        |> Async.StartAsPromise
        |> Promise.map (fun (exitCode : int) -> Expect.equal exitCode 0 "Tests failed")
    )
)
#else
[<EntryPoint>]
let main argv = Pyxpecto.runTests [||] all
#endif