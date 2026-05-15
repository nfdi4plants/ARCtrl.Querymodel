module ArcTables.Tests

open Fable.Pyxpecto
open ARCtrl
open ARCtrl.QueryModel

let data =
    
    testList "Data" [
        testCase "OutputDataInputSample" <| fun _ ->
            let t = ArcTable.init("MyTable")
            t.AddColumn(CompositeHeader.Input IOType.Sample, ResizeArray [CompositeCell.FreeText "InputSample"])
            t.AddColumn(CompositeHeader.Output IOType.Data, ResizeArray [CompositeCell.FreeText "OutputData"])             
            let dataNodes = ArcTables(ResizeArray[t]).Data
            Expect.hasLength dataNodes 1 "Data should have one row"
            let data = dataNodes |> Seq.head
            Expect.equal data.Name "OutputData" "Data node should be named OutputData"
            Expect.isTrue data.isData "Data node should be recognized as data"
    ]

let subtree = 
    testList "SubTree" [
        testCase "OutputDataInputSample" <| fun _ ->
            let t = ArcTable.init("MyTable")
            t.AddColumn(CompositeHeader.Input IOType.Sample, ResizeArray [CompositeCell.FreeText "InputSample1"; CompositeCell.FreeText "InputSample2"])
            t.AddColumn(CompositeHeader.Output IOType.Data, ResizeArray [CompositeCell.createDataFromString "OutputData1"; CompositeCell.createDataFromString "OutputData2"])             
            Expect.equal t.RowCount 2 "Table should have two rows"
            let tables = ArcTables(ResizeArray[t])
            let subTree = ArcTables.getSubTreeOf "OutputData2" tables
            let t' = subTree.[0]
            Expect.equal t'.RowCount 1 "Subtree table should have one row"
    ]


let main = testList "ArcTables" [
    data
    subtree
]