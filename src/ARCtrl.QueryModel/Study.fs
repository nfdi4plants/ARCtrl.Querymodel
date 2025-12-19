namespace ARCtrl.QueryModel

open ARCtrl
open System.IO

open System.Collections.Generic
open System.Collections

[<AutoOpen>]
module ArcStudyExtensions = 

    /// Queryable representation of an ISA Study. Implements the QProcessSequence interface
    type ArcStudy with 

        //static member fromStudy (study : Study, ?ReferenceSheets : QSheet list) =
        
        //    let comments = QCommentCollection(study.Comments)
            
        //    let refSheets = 
        //        study.Assays 
        //        |> Option.map (List.collect (fun a -> a.ProcessSequence |> Option.defaultValue []) )
        //        |> Option.defaultValue []
        //        |> List.append (study.ProcessSequence |> Option.defaultValue [])
        //        |> fun s ->
        //            match ReferenceSheets with
        //            | Some ref -> QProcessSequence(s,ref)
        //            | None -> QProcessSequence(s)
        //        |> Seq.toList

        //    let sheets = QProcessSequence(study.ProcessSequence |> Option.defaultValue [],refSheets) |> Seq.toList

        //    let assays = 
        //        study.Assays 
        //        |> Option.map (List.map (fun a -> QAssay.fromAssay(a,refSheets)))
        //        |> Option.defaultValue []

        //    QStudy(study.FileName,study.Identifier,study.Title,study.Description,study.SubmissionDate,study.PublicReleaseDate,study.Publications,study.Contacts,study.StudyDesignDescriptors,comments,assays,sheets)

        //member this.FullProcessSequence =        
        //    this.Assays
        //    |> List.collect (fun a -> a.Sheets)
        //    |> List.append this.Sheets
        //    |> QProcessSequence


        /// get the protocol or sheet (in ISATab logic) with the given name
        member this.Protocol (sheetName : string) =
            this.GetTable sheetName

        /// get the nth protocol or sheet (in ISATab logic) 
        member this.Protocol (index : int) =
            this.GetTableAt index

        /// Returns the initial inputs final outputs of the assay, to which no processPoints
        static member getRootInputs (study : ArcStudy) = ArcTables.getRootInputs study

        /// Returns the final outputs of the study, which point to no further nodes
        static member getFinalOutputs (study : ArcStudy) = ArcTables.getFinalOutputs study

        /// Returns the initial inputs final outputs of the study, to which no processPoints
        static member getRootInputOf (study : ArcStudy) (sample : string) = ArcTables.getRootInputsOfBy (fun _ -> true) sample study 
        
        /// Returns the final outputs of the study, which point to no further nodes
        static member getFinalOutputsOf (study : ArcStudy) (sample : string) = ArcTables.getFinalOutputsOfBy (fun _ -> true) sample study

module Study =

    open Errors

    //let fileName (s : ArcStudy) = 
    //    s.FileName
        //match s.FileName with
        //| Some v -> v
        //| None -> raise StudyHasNoFileNameException
    let identifier (s : ArcStudy) = 
        s.Identifier
        //match s.Identifier with
        //| Some v -> v
        //| None -> raise StudyHasNoIdentifierException
    let title (s : ArcStudy) = 
        match s.Title with
        | Some v -> v
        | None -> raise StudyHasNoTitleException
    let description (s : ArcStudy) = 
        match s.Description with
        | Some v -> v
        | None -> raise StudyHasNoDescriptionException
    let submissionDate (s : ArcStudy) = 
        match s.SubmissionDate with
        | Some v -> v
        | None -> raise StudyHasNoSubmissionDateException
    let publicReleaseDate (s : ArcStudy) = 
        match s.PublicReleaseDate with
        | Some v -> v
        | None -> raise StudyHasNoPublicReleaseDateException
    let publications (s : ArcStudy) = 
        match s.Publications with
        | a when a.Count = 0 -> raise StudyHasNoPublicationsException
        | a -> a
    let contacts (s : ArcStudy) = 
        match s.Contacts with
        | a when a.Count = 0 -> raise StudyHasNoContactsException
        | v -> v
    let designDescriptors (s : ArcStudy) = 
        match s.StudyDesignDescriptors with
        | a when a.Count = 0 -> raise StudyHasNoDesignDescriptorsException
        | v -> v
    //let protocols (s : ArcStudy) = 
    //    match s.proto with
    //    | [] -> raise StudyHasNoProtocolsException
    //    | v -> v
    //let materials (s : QStudy) = 
    //    match s.Materials with
    //    | Some v -> v
    //    | None -> raise StudyHasNoMaterialsException
    //let fileName (s : QStudy) = 
    //    match s.Protocols with
    //    | Some v -> v
    //    | None -> raise StudyHasNoProcessSequenceException
    let assays (s : ArcStudy) = 
        if s.RegisteredAssays.Count = 0 then raise StudyHasNoAssaysException
        else s.RegisteredAssays
       
    //let factors (s : QStudy) = 
    //    let f = s.Factors()
    //    if f.IsEmpty then raise StudyHasNoFactorsException
    //let CharacteristicCategories (s : QStudy) = 
    //    match s.FileName with
    //    | Some v -> v
    //    | None -> raise StudyHasNoCharacteristicCategoriesException
    //let fileName (s : QStudy) = 
    //    match s.FileName with
    //    | Some v -> v
    //    | None -> raise StudyHasNoUnitCategoriesException
    //let fileName (s : QStudy) = 
    //    match s.FileName with
    //    | Some v -> v
    //    | None -> raise StudyHasNoStudyHasNoCommentsExceptionException
