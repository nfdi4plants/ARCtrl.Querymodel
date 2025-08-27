module ARCtrl.Fragcess.FragmentSelector

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
        