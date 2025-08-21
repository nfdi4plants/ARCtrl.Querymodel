namespace ARCtrl.QueryModel

open ARCtrl
open System.Text.Json.Serialization
open System.Text.Json
open System.IO

open System.Collections.Generic
open System.Collections

[<AutoOpen>]
module ArcInvestigationExtensions = 

    let dedupeName (getter : 'T -> string) (setter :  string -> 'T -> 'T) (elements : 'T seq) =
        let dict = new Dictionary<string, int>()
        [
            for e in elements do
                let name = getter e
                if dict.ContainsKey(name) then
                    let count = dict.[name]
                    dict.[name] <- count + 1
                    setter (name + " " + count.ToString()) e

                else
                    dict.Add(name, 1)
                    e
        ]

    /// Queryable representation of an ISA Investigation. Implements the QProcessSequence interface
    type ArcInvestigation with

        /// Returns the QStudy with the given name
        member this.ArcTables
            with get() : ArcTables = 
                seq {
                    for s in this.Studies do yield! s.Tables
                    for a in this.Assays do yield! a.Tables
                }
                |> dedupeName (fun (a : ArcTable) -> a.Name) (fun v (a : ArcTable) -> ArcTable.fromArcTableValues(v,a.Headers,a.Values))
                |> ResizeArray
                |> ArcTables

    module Investigation =

        open Errors

        //let fileName (i : ArcInvestigation) =
        //    match i.FileName with
        //    | Some v -> (v)
        //    | None -> raise InvestigationHasNoFileNameException
        let identifier (i : ArcInvestigation) =
            i.Identifier
        let title (i : ArcInvestigation) =
            match i.Title with
            | Some v -> (v)
            | None -> raise InvestigationHasNoTitleException
        let description (i : ArcInvestigation) =
            match i.Description with
            | Some v -> (v)
            | None -> raise InvestigationHasNoDescriptionException
        let submissionDate (i : ArcInvestigation) =
            match i.SubmissionDate with
            | Some v -> (v)
            | None -> raise InvestigationHasNoSubmissionDateException
        let publicReleaseDate (i : ArcInvestigation) =
            match i.PublicReleaseDate with
            | Some v -> (v)
            | None -> raise InvestigationHasNoPublicReleaseDateException
        let ontologySourceReferences (i : ArcInvestigation) =
            match i.OntologySourceReferences with
            | a when a.Count = 0 -> raise InvestigationHasNoOntologySourceReferencesException
            | v -> v
        let publications (i : ArcInvestigation) =
            match i.Publications with
            | a when a.Count = 0 -> raise InvestigationHasNoPublicationsException
            | v -> (v)
        let contacts (i : ArcInvestigation) =
            match i.Contacts with
            | a when a.Count = 0 -> raise InvestigationHasNoContactsException
            | v -> (v)
