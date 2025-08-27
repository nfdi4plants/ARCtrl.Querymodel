module FragmentSelector.Tests

open Expecto
open ARCtrl
open ARCtrl.Fragcess
let testArcPath = __SOURCE_DIRECTORY__ + @"\TestObjects\TestArc"
let testArc = ARC.load(testArcPath)

let csv_tests =
    
    testList "text/csv" [
        testCase "col" <| fun _ ->
            let s = "col=3"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.Column 3
            Expect.equal result expected "col=3"
        testCase "row" <| fun _ ->
            let s = "row=5"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.Row 5
            Expect.equal result expected "row=5"
        testCase "cell" <| fun _ ->
            let s = "cell=2,4"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.Cell (2,4)
            Expect.equal result expected "cell=2,4"
        testCase "row range" <| fun _ ->
            let s = "row=3-7"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.RowRange (3,7)
            Expect.equal result expected "row=3-7"
        testCase "column range" <| fun _ ->
            let s = "col=1-4"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.ColumnRange (1,4)
            Expect.equal result expected "col=1-4"
        testCase "cell range" <| fun _ ->
            let s = "cell=2,3-5,7"
            let result = FragmentSelector.CSV.fromString(s)
            let expected = FragmentSelector.CellRange ((2,3),(5,7))
            Expect.equal result expected "cell=2,3-5,7"
    ]


[<Tests>]
let main = testList "FragmentSelectorTests" [
    csv_tests
]