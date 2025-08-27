namespace ARCtrl.Fragcess

open Deedle
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



[<AutoOpen>]
module ARCExtensions = 

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

module DataContext = 
    
    let getAbsolutePath (basePath : string) (dc : DataContext) =
        match dc.FilePath with
        | Some fp when System.IO.Path.IsPathRooted(fp) -> fp
        | Some fp -> System.IO.Path.Combine(basePath,fp)
        | None -> basePath
