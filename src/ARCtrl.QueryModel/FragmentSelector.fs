module ARCtrl.QueryModel.FragmentSelector

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

let (|Regex|_|) pattern s = 
    let r = System.Text.RegularExpressions.Regex.Match(s, pattern)
    if r.Success then Some(r)
    else None

type CSV = 
    | Row of int
    | Column of int
    | Cell of int * int
    | RowRange of int * int
    | ColumnRange of int * int
    | CellRange of (int * int) * (int * int)

    member this.IsIncludedIn (outer : CSV) =
        match outer, this with
        | _ when outer = this -> true
        | RowRange (startRow1, endRow1), Row r2 -> r2 >= startRow1 && r2 <= endRow1
        | ColumnRange (startCol1, endCol1), Column c2 -> c2 >= startCol1 && c2 <= endCol1
        | CellRange ((startRow1, startCol1), (endRow1, endCol1)), Cell (r2,c2) -> r2 >= startRow1 && r2 <= endRow1 && c2 >= startCol1 && c2 <= endCol1 
        | _ -> false

    static member isIncluded (outer : string) (inner : string) =
        let outerSelector : CSV  = CSV.fromString outer
        let innerSelector : CSV = CSV.fromString inner
        innerSelector.IsIncludedIn outerSelector

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

    static member getZeroBasedColumnIndexFromString(s : string) =
        match s with
        | Regex columnRegex m -> 
            let column = m.Value |> int
            column - 1
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv column." s

    static member getZeroBasedRowIndexFromString(s : string) =
        match s with
        | Regex rowRegex m -> 
            let row = m.Value |> int
            row - 1
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv row." s

    static member getZeroBasedCellIndexFromString(s : string) =
        match s with
        | Regex cellRegex m -> 
            let row = (m.Groups.["row"].Value |> int) - 1
            let column = (m.Groups.["column"].Value |> int) - 1
            (row, column)
        | _ -> failwithf "Fragment Selector \"%s\" could not be parsed as text/csv cell." s