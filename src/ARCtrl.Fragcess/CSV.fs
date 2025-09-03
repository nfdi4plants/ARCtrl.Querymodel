namespace ARCtrl.Fragcess

open Deedle
open ARCtrl
open ARCtrl.QueryModel
open FragmentSelector

module Frame = 
    
    let inline getCellAtAs<'T> (f : Frame<'R,string>) (r : int) (c : int) : 'T =
        let c : Series<'R,'T> = f.GetColumnAt<'T>(c)
        c.GetAt(r)
        
    let getCellAt (f : Frame<'R,'C>) (r : int) (c : int) : obj =
        f.GetColumnAt(c).GetAt(r)


module CSV = 

    let getObjectFromFrame (dc : DataContext) (f : Frame<int,'C>) : obj =
        match dc.Selector with
        | Some s -> 
            let selector = CSV.fromString s
            match selector with
            | CSV.Column col -> 
                match dc.ObjectType with
                | Some oa when ObjectType.isString oa -> f.GetColumnAt<string>(col)
                | Some oa when ObjectType.isFloat oa -> f.GetColumnAt<float>(col)
                | Some oa when ObjectType.isInt oa -> f.GetColumnAt<int>(col)
                | _ -> f.GetColumnAt(col)
            | CSV.Cell (r,c) -> 
                match dc.ObjectType with
                | Some oa when ObjectType.isString oa -> Frame.getCellAtAs<string> f r c
                | Some oa when ObjectType.isFloat oa -> Frame.getCellAtAs<float> f r c
                | Some oa when ObjectType.isInt oa -> Frame.getCellAtAs<int> f r c
                | _ -> Frame.getCellAt f r c
            | CSV.Row row ->
                match dc.ObjectType with
                | Some oa when ObjectType.isString oa -> f.GetRowAt<string>(row)
                | Some oa when ObjectType.isFloat oa -> f.GetRowAt<float>(row)
                | Some oa when ObjectType.isInt oa -> f.GetRowAt<int>(row)
                | _ -> f.GetRowAt(row)
            | CSV.RowRange (startRow, endRow) ->
                failwithf "RowRange selector not implemented yet"
            | CSV.ColumnRange (startCol, endCol) ->
                failwithf "ColumnRange selector not implemented yet"
            | CSV.CellRange ((startRow, endRow),(startCol, endCol))->
                failwithf "Cellrange selector not implemented yet"       
        | None -> f