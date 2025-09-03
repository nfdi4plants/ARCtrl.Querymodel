namespace ARCtrl.QueryModel

open ARCtrl

module ObjectType = 
    
    let isString (ot : OntologyAnnotation) =
        //let tanInfoMatches (tanInfo : {|IDSpace : string; LocalID : string|}) =
        //    tanInfo.IDSpace = 
        //match ot.Name, ot.TANInfo with
        //| Some "string", Some tanInfo when tanInfo.IDSpace =  -> true
        match ot.Name with
        | Some "string" | Some "String" -> true
        | _ -> false

    let isFloat (ot : OntologyAnnotation) =
        match ot.Name with
        | Some "Float" | Some "float" | Some "double" | Some "Double" -> true
        | _ -> false

    let isInt (ot : OntologyAnnotation) =
        match ot.Name with
        | Some "int" | Some "Int" | Some "integer" | Some "Integer" -> true
        | _ -> false

module DataContext = 
    
    let dataContexts : ResizeArray<DataContext> = ResizeArray()

    let selectorIsIncluded (outer : DataContext) (inner : DataContext) =
        match outer.Selector, inner.Selector with
        | Some o, Some i when o = i -> true
        | Some o, Some i -> FragmentSelector.CSV.isIncluded o i
        | _ -> false

    let tryGetDataContextByName (name : string) =
        if name.Contains("#") then 
            let [|path;selector|] = name.Split('#')
            dataContexts
            |> Seq.tryFind (fun n -> 
                if n.FilePath.IsNone || n.Selector.IsNone then false
                else 
                    n.FilePath.Value = path && (n.Selector.Value = selector || FragmentSelector.CSV.isIncluded n.Selector.Value selector)
                )
        else
            dataContexts
            |> Seq.tryFind (fun n -> n.NameText = name)


    let getAbsolutePath (basePath : string) (dc : DataContext) =
        match dc.FilePath with
        | Some fp when System.IO.Path.IsPathRooted(fp) -> fp
        | Some fp -> System.IO.Path.Combine(basePath,fp)
        | None -> basePath

[<AutoOpen>]
module ARCExtensions = 

    type DataContext with

        member this.ExplicationEquals (explication : OntologyAnnotation) =
            this.Explication.IsSome && this.Explication.Value.Equals explication

    type ARC with

        member this.GetDataContextMap() =
            let dcs = 
                this.Assays |> Seq.choose (fun a -> a.DataMap)
                |> Seq.append (this.Studies  |> Seq.choose (fun s -> s.DataMap))
                |> Seq.collect (fun dm -> dm.DataContexts)
            dcs
            |> Seq.choose (fun dc -> 
                dc.Name 
                |> Option.map (fun name -> name,dc))
            |> Helper.Dictionary.ofSeq

        member this.DataContextMapping() =
           this.Assays |> Seq.iter (fun a -> if a.DataMap.IsSome then DataContext.dataContexts.AddRange a.DataMap.Value.DataContexts)
           this.Studies |> Seq.iter (fun s -> if s.DataMap.IsSome then DataContext.dataContexts.AddRange s.DataMap.Value.DataContexts)