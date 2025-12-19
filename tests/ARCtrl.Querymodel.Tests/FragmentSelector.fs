module FragmentSelector.Tests

open Fable.Pyxpecto
open ARCtrl
open ARCtrl.QueryModel

let csv_tests =
    
    testList "text/csv" [
        testList "fromString" [
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
        testList "isIncludedString" [
            testCase "equalCols" <| fun _ ->
                let selector = "col=5"
                let result = FragmentSelector.CSV.isIncludedString selector selector
                Expect.isTrue result "Equal cols are handled correctly"
            testCase "equalRows" <| fun _ ->
                let selector = "row=2"
                let result = FragmentSelector.CSV.isIncludedString selector selector
                Expect.isTrue result "Equal rows are handled correctly"
            testCase "equalCells" <| fun _ ->
                let selector = "cell=3,4"
                let result = FragmentSelector.CSV.isIncludedString selector selector
                Expect.isTrue result "Equal cells are handled correctly"
            testCase "unequalCols" <| fun _ ->
                let outer = "col=2"
                let inner = "col=5"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Unequal cols are handled correctly"
            testCase "unequalRows" <| fun _ ->
                let outer = "row=3"
                let inner = "row=7"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Unequal rows are handled correctly"
            testCase "unequalCells" <| fun _ ->
                let outer = "cell=2,3"
                let inner = "cell=5,7"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Unequal cells are handled correctly"
            testCase "unequalSelectors" <| fun _ ->
                let outer = "col=2"
                let inner = "row=5"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Unequal selectors are handled correctly"
            testCase "colInColRange" <| fun _ ->
                let outer = "col=2-5"
                let inner = "col=3"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isTrue result "Col in col range is handled correctly"
            testCase "colNotInColRange" <| fun _ ->
                let outer = "col=2-5"
                let inner = "col=7"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Col not in col range is handled correctly"
            testCase "rowInRowRange" <| fun _ ->
                let outer = "row=3-8"
                let inner = "row=5"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isTrue result "Row in row range is handled correctly"
            testCase "rowNotInRowRange" <| fun _ ->
                let outer = "row=3-8"
                let inner = "row=2"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Row not in row range is handled correctly"
            testCase "cellInCellRange" <| fun _ ->
                let outer = "cell=2,3-5,7"
                let inner = "cell=3,5"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isTrue result "Cell in cell range is handled correctly"
            testCase "cellNotInCellRange" <| fun _ ->
                let outer = "cell=2,3-5,7"
                let inner = "cell=6,8"
                let result = FragmentSelector.CSV.isIncludedString outer inner
                Expect.isFalse result "Cell not in cell range is handled correctly"                
        ]
        testList "getZeroBasedIndicesFromString" [
            testCase "column" <| fun _ ->
                let selector = "col=3"
                let result = FragmentSelector.CSV.getZeroBasedColumnIndexFromString selector
                let expected = 2
                Expect.equal result expected "Column selector"
            testCase "row" <| fun _ ->
                let selector = "row=5"
                let result = FragmentSelector.CSV.getZeroBasedRowIndexFromString selector
                let expected = 4
                Expect.equal result expected "Row selector"
            testCase "cell" <| fun _ ->
                let selector = "cell=2,4"
                let result = FragmentSelector.CSV.getZeroBasedCellIndexFromString selector
                let expected = (1,3)
                Expect.equal result expected "Cell selector"
            
        
        ]
    ]

let main = testList "FragmentSelectorTests" [
    csv_tests
]