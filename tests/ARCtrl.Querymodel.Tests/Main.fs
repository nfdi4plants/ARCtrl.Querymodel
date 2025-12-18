module arcIO.NET.Tests

open Fable.Pyxpecto
open Fable.Core


let all = testSequenced <| testList "All" [
        TestARC.Tests.main     
        FragmentSelector.Tests.main
    ]

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open TestingUtils.TSUtils

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