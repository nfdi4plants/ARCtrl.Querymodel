module ProcessCore.Tests

open Fable.Pyxpecto
open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel.ProcessCore

let constructors =
    
    testList "constructors" [
        testCase "QLabProcess" <| fun _ ->
            let node = LDLabProcess.create(name = "Test Process", id = "#Test_Process_ID")
            let p = QLabProcess(node)
            Expect.equal p.Id "#Test_Process_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test Process" "Name property should be set correctly"
        testCase "IONode" <| fun _ ->
            let node = LDSample.create(name = "Test IO Node", id = "#Test_IO_Node_ID")
            let p = IONode(node)
            Expect.equal p.Id "#Test_IO_Node_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test IO Node" "Name property should be set correctly"
        testCase "QLabProtocol" <| fun _ ->
            let node = LDLabProtocol.create(name = "Test QLab Protocol", id = "#Test_QLab_Protocol_ID")
            let p = QLabProtocol(node)
            Expect.equal p.Id "#Test_QLab_Protocol_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test QLab Protocol" "Name property should be set correctly"
    ]

let main = testList "ProcessCore" [
    constructors
]