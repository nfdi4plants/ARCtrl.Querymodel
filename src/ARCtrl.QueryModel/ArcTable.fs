namespace ARCtrl.QueryModel

open ARCtrl
open ARCtrl.Process
//
//open System.Text.Json
open System.IO

open System.Collections.Generic
open System.Collections


[<AutoOpen>]
module ProtocolExtensions = 

    //type Protocol with
    
        //static member rowIndexKeyName = "RowIndex"

        //member this.SetRowIndex(index : int) = 
        //    let c = Comment.create(Name = Protocol.rowIndexKeyName,Value = string index)
        //    let cs = 
        //        this.Comments 
        //        |> Option.defaultValue []
        //        |> CommentList.set c
        //    Protocol.setComments this cs

        //member this.GetRowIndex() =
        //    match this.Comments with
        //    | Some cs -> cs |> API.CommentList.item Protocol.rowIndexKeyName |> int
        //    | None -> failwith "protocol does not contain any comments, so no rowIndex could be returned"
            
        //member this.TryGetRowIndex() =
        //    this.Comments
        //    |> Option.bind (API.CommentList.tryItem Protocol.rowIndexKeyName)
        //    |> Option.map (int)

        //static member setRowIndex i (p : Protocol) = p.SetRowIndex(i)

        //static member rowRangeKeyName = "RowRange"

        //static member composeRowRange (from : int) (to_ : int) =
        //    $"{from}:{to_}"

        //static member decomposeRowRange (range : string) =
        //    let pattern = """(?<from>\d+):(?<to>\d+)"""
        //    let r = System.Text.RegularExpressions.Regex.Match(range,pattern)

        //    if r.Success then
        //        (r.Groups.Item "from" ).Value |> int, (r.Groups.Item "to").Value |> int
        //    else 
        //        failwithf "protocol rowRange %s could not be parsed. It should be of form \"from:to\" (e.g. 0:10)" range

        //member this.SetRowRange(range : string) = 
        //    let c = Comment.create(Name = Protocol.rowRangeKeyName,Value = range)
        //    let cs = 
        //        this.Comments 
        //        |> Option.defaultValue []
        //        |> API.CommentList.set c
        //    API.Protocol.setComments this cs

        //member this.SetRowRange(from : int, to_ : int) = 
        //    Protocol.composeRowRange from to_
        //    |> this.SetRowRange

        //member this.GetRowRange() =
        //    match this.Comments with
        //    | Some cs -> cs |> API.CommentList.item Protocol.rowRangeKeyName |> Protocol.decomposeRowRange
        //    | None -> failwith "protocol does not contain any comments, so no rowRange could be returned"
            
        //member this.TryGetRowRange() =
        //    this.Comments
        //    |> Option.bind (API.CommentList.tryItem Protocol.rowRangeKeyName)
        //    |> Option.map (Protocol.decomposeRowRange)

        //static member setRowRange (range : string) = fun (p : Protocol) -> p.SetRowRange(range)

        //static member setRowRange (from : int,to_ : int) = fun (p : Protocol) -> p.SetRowRange(from,to_)

        //static member dropRowIndex (p : Protocol) =
        //    match p.Comments with 
        //    | None -> p
        //    | Some cs ->
        //        API.CommentList.dropByKey Protocol.rowIndexKeyName cs
        //        |> Option.fromValueWithDefault []
        //        |> fun cs -> {p with Comments = cs}

        //static member rangeOfIndices (i : int list) =
        //    Protocol.composeRowRange (List.min i) (List.max i)

        //static member mergeIndicesToRange (ps : Protocol list) =
        //    let indices = ps |> List.choose (fun p -> p.TryGetRowIndex())
        //    if indices.IsEmpty then ps.[0]
        //    else
        //        let r = indices |> Protocol.rangeOfIndices
        //        ps.[0].SetRowRange r
        //        |> Protocol.dropRowIndex

        //member this.IsChildProtocolOf(parentProtocolType : OntologyAnnotation) =
        //    match this.ProtocolType with
        //    | Some pt ->
        //        OntologyAnnotation.isChildTerm(parentProtocolType,pt)
        //    | _ -> false

        //member this.IsChildProtocolOf(parentProtocolType : OntologyAnnotation, obo : OBO.NET.OboOntology) =
        //    match this.ProtocolType with
        //    | Some pt ->
        //        OntologyAnnotation.isChildTerm(parentProtocolType,pt,obo)
        //    | _ -> false

    
    type ProtocolDescriptor<'T> =
        | ForAll of 'T
        | ForSpecific of Map<int,'T>

        with member this.TryGet(i) =
                match this with
                | ForAll x -> Some x
                | ForSpecific m -> Map.tryFind i m


[<AutoOpen>]
module ArcTableExtensions = 

    

    type CRow(vals : seq<(CompositeHeader * CompositeCell)>) = 

        member this.Cells = vals

        static member input (row : CRow) = 
            row.Cells
            |> Seq.pick (fun (header,cell) -> if header.isInput then Some cell else None)

        static member output (row : CRow) =
            row.Cells
            |> Seq.pick (fun (header,cell) -> if header.isOutput then Some cell else None)

        static member inputName (row : CRow) =
            row.Cells
            |> Seq.pick (fun (header,cell) -> if header.isInput then cell.GetContent().[0] |> Some else None)

        static member outputName (row : CRow) =
            row.Cells
            |> Seq.pick (fun (header,cell) -> if header.isOutput then cell.GetContent().[0] |> Some else None)

        static member inputType (row : CRow)  =
            row.Cells
            |> Seq.pick (fun (header,_) -> header.TryInput())

        static member outputType (row : CRow) =
            row.Cells
            |> Seq.pick (fun (header,_) -> header.TryOutput())
            

        member this.InputName = CRow.inputName this

        member this.OutputName = CRow.outputName this

        member this.Input = this.InputName

        member this.Output = this.OutputName

        member this.InputType = CRow.inputType this

        member this.OutputType = CRow.outputType this

        member this.Values = 
            this.Cells
            |> Seq.choose (fun (header,cell) ->
                ISAValue.tryCompose header cell                                       
            )

/// Queryable type representing a collection of processes implementing the same protocol. Or in ISAtab / ISAXLSX logic a sheet in an assay or study file.
///
/// Values are represented rowwise with input and output entities.
    type ArcTable with
    
        member this.Rows = 
            [
                for i = 0 to this.RowCount - 1 do
                    Seq.zip this.Headers (this.GetRow i)
                    |> CRow

            ]
            
        member this.ISAValues =          
            this.Rows
            |> Seq.collect (fun r -> 
                let i = r.InputName
                let o = r.OutputName
                r.Cells
                |> Seq.choose (fun (header,cell) ->
                    ISAValue.tryCompose header cell
                    |> Option.map (fun v -> KeyValuePair ((i,o),v))                                            
                )
                
            )
            |> Seq.toList
            |> IOValueCollection

        static member inputType (t : ArcTable)  =
            t.Headers
            |> Seq.pick (fun header -> header.TryInput())

        static member outputType (t : ArcTable) =
            t.Headers
            |> Seq.pick (fun header -> header.TryOutput())

        member this.InputType = ArcTable.inputType this

        member this.OutputType = ArcTable.outputType this

        //member this.TryGetChildProtocolTypeOf(parentProtocolType : OntologyAnnotation) =
        //    this.Protocols
        //    |> List.choose (fun p -> if p.IsChildProtocolTypeOf(parentProtocolType) then Some p else None)
        //    |> Option.fromValueWithDefault []

        //member this.TryGetChildProtocolTypeOf(parentProtocolType : OntologyAnnotation, obo : Obo.OboOntology) =
        //    this.Protocols
        //    |> List.choose (fun p -> if p.IsChildProtocolTypeOf(parentProtocolType, obo) then Some p else None)
        //    |> Option.fromValueWithDefault []

        member this.Item (i : int) =
            this.Rows.[i]

        member this.Item (input : string) =
            let row = 
                this.Rows 
                |> List.tryFind (CRow.inputName >> (=) input)
            match row with
            | Some r -> r
            | None -> failwith $"Sheet \"{this.Name}\" does not contain row with input \"{input}\""

        member this.RowCount =
            this.Rows 
            |> List.length

        member this.InputNames =
            this.Rows 
            |> List.map CRow.inputName

        member this.OutputNames =
            this.Rows 
            |> List.map CRow.outputName
    
        member this.Inputs =
            this.Rows 
            |> List.map (fun row -> CRow.inputName row, CRow.inputType row)

        member this.Outputs =
            this.Rows 
            |> List.map (fun row -> CRow.outputName row, CRow.outputType row)

   