module ARCtrl.QueryModel.FragmentSelector

open ARCtrl.Helper.Regex.ActivePatterns
open Fable.Core

[<Literal>]
let rowRegex = """(?<=row=)\d*$"""

[<Literal>]
let columnRegex = """(?<=col=)\d*$"""

[<Literal>]
let cellRegex = """(?<=cell=)(?<row>\d*),(?<column>\d*)$"""

[<Literal>]
let rowRangeRegex = """(?<=row=)(?<start>\d*)-(?<end>\d*)$"""

[<Literal>]
let columnRangeRegex = """(?<=col=)(?<start>\d*)-(?<end>\d*)$"""

[<Literal>]
let cellRangeRegex = """(?<=cell=)(?<startRow>\d*),(?<startColumn>\d*)-(?<endRow>\d*),(?<endColumn>\d*)$"""

//ARCtrl.Helper.Regex.ActivePatterns.Regex

//let (|Regex|_|) pattern s = 
//    let r = System.Text.RegularExpressions.Regex.Match(s, pattern)
//    if r.Success then Some(r)
//    else None

[<AttachMembers>]
type CSV = 
    | Row of int
    | Column of int
    | Cell of int * int
    | RowRange of int * int
    | ColumnRange of int * int
    | CellRange of (int * int) * (int * int)

    /// Check if this selector is included in the outer selector
    ///
    /// E.g. Row 3 is included in RowRange (1,5)
    member this.IsIncludedIn (outer : CSV) =
        match outer, this with
        | _ when outer = this -> true
        | RowRange (startRow1, endRow1), Row r2 -> r2 >= startRow1 && r2 <= endRow1
        | ColumnRange (startCol1, endCol1), Column c2 -> c2 >= startCol1 && c2 <= endCol1
        | CellRange ((startRow1, startCol1), (endRow1, endCol1)), Cell (r2,c2) -> r2 >= startRow1 && r2 <= endRow1 && c2 >= startCol1 && c2 <= endCol1 
        | _ -> false

    /// Check if the inner selector (as string) is included in the outer selector (as string)
    ///
    /// E.g. "row=3" is included in "row=1-5"
    static member isIncludedString (outer : string) (inner : string) =
        let outerSelector : CSV  = CSV.fromString outer
        let innerSelector : CSV = CSV.fromString inner
        innerSelector.IsIncludedIn outerSelector

    /// Parse a CSV fragment selector from string (1-based indexing)
    ///
    /// https://datatracker.ietf.org/doc/html/rfc7111
    static member fromString(s : string) =
        match s with
        | Regex rowRegex m -> 
            let row = m.Value |> int
            Row row
        | Regex columnRegex m -> 
            let column = m.Value |> int
            Column column
        | Regex cellRegex m -> 
            let row = m.Groups.["row"].Value |> int
            let column = m.Groups.["column"].Value |> int
            Cell (row, column)
        | Regex rowRangeRegex m -> 
            let startRow = m.Groups.["start"].Value |> int
            let endRow = m.Groups.["end"].Value |> int
            RowRange (startRow, endRow)
        | Regex columnRangeRegex m ->
            let startColumn = m.Groups.["start"].Value |> int
            let endColumn = m.Groups.["end"].Value |> int
            ColumnRange (startColumn, endColumn)
        | Regex cellRangeRegex m ->
            let startRow = m.Groups.["startRow"].Value |> int
            let startColumn = m.Groups.["startColumn"].Value |> int
            let endRow = m.Groups.["endRow"].Value |> int
            let endColumn = m.Groups.["endColumn"].Value |> int
            CellRange ((startRow, startColumn), (endRow, endColumn))
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv." s
        
    /// Parse a CSV fragment selector from string (0-based indexing)
    ///
    /// https://datatracker.ietf.org/doc/html/rfc7111
    static member fromStringZeroBased(s : string) = 
        match s with
        | Regex rowRegex m -> 
            let row = (m.Value |> int) - 1
            Row row
        | Regex columnRegex m -> 
            let column = (m.Value |> int) - 1
            Column column
        | Regex cellRegex m -> 
            let row = (m.Groups.["row"].Value |> int) - 1
            let column = (m.Groups.["column"].Value |> int) - 1
            Cell (row, column)
        | Regex rowRangeRegex m -> 
            let startRow = (m.Groups.["start"].Value |> int) - 1
            let endRow = (m.Groups.["end"].Value |> int) - 1
            RowRange (startRow, endRow)
        | Regex columnRangeRegex m ->
            let startColumn = (m.Groups.["start"].Value |> int) - 1
            let endColumn = (m.Groups.["end"].Value |> int) - 1
            ColumnRange (startColumn, endColumn)
        | Regex cellRangeRegex m ->
            let startRow = (m.Groups.["startRow"].Value |> int) - 1
            let startColumn = (m.Groups.["startColumn"].Value |> int) - 1
            let endRow = (m.Groups.["endRow"].Value |> int) - 1
            let endColumn = (m.Groups.["endColumn"].Value |> int) - 1
            CellRange ((startRow, startColumn), (endRow, endColumn))
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv." s

    /// Get the zero-based column index from a CSV fragment selector string. If it is not a column selector, an exception is raised
    static member getZeroBasedColumnIndexFromString(s : string) =
        match s with
        | Regex columnRegex m -> 
            let column = m.Value |> int
            column - 1
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv column." s

    /// Get the zero-based row index from a CSV fragment selector string. If it is not a row selector, an exception is raised
    static member getZeroBasedRowIndexFromString(s : string) =
        match s with
        | Regex rowRegex m -> 
            let row = m.Value |> int
            row - 1
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv row." s

    /// Get the zero-based cell index from a CSV fragment selector string. If it is not a cell selector, an exception is raised
    static member getZeroBasedCellIndexFromString(s : string) =
        match s with
        | Regex cellRegex m -> 
            let row = (m.Groups.["row"].Value |> int) - 1
            let column = (m.Groups.["column"].Value |> int) - 1
            (row, column)
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv cell." s