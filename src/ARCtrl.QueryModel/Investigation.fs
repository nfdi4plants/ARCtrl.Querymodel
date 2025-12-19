namespace ARCtrl.QueryModel

open ARCtrl
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

        /// Returns all values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousValuesOf(node : QNode, ?ProtocolName, ?StudyName, ?AssayName) =
            match StudyName, AssayName with
            | Some s, Some a -> failwithf "Could not retreive previous of node %s: Cannot specify both StudyName and AssayName" node.Name
            | Some s, None -> 
                let previousNodes = ArcTables.getPreviousNodesBy node.Name this.ArcTables
                let s = this.GetStudy(s)
                previousNodes
                |> List.collect (fun n -> s.PreviousValuesOf(n, ?ProtocolName = ProtocolName).Values)
                |> ValueCollection
            | None, Some a -> 
                let previousNodes = ArcTables.getPreviousNodesBy node.Name this.ArcTables
                let a = this.GetAssay(a)
                previousNodes
                |> List.collect (fun n -> a.PreviousValuesOf(n, ?ProtocolName = ProtocolName).Values)
                |> ValueCollection               
            | None, None ->
                this.ArcTables.PreviousValuesOf(node.Name, ?ProtocolName = ProtocolName)

        member this.PreviousCharacteristicsOf(node : QNode, ?ProtocolName, ?StudyName, ?AssayName) =
            this.PreviousValuesOf(node, ?ProtocolName = ProtocolName, ?StudyName = StudyName, ?AssayName = AssayName).Characteristics()

        member this.PreviousFactorsOf(node : QNode, ?ProtocolName, ?StudyName, ?AssayName) =
            this.PreviousValuesOf(node, ?ProtocolName = ProtocolName, ?StudyName = StudyName, ?AssayName = AssayName).Factors()

        member this.PreviousParametersOf(node : QNode, ?ProtocolName, ?StudyName, ?AssayName) =
            this.PreviousValuesOf(node, ?ProtocolName = ProtocolName, ?StudyName = StudyName, ?AssayName = AssayName).Parameters()

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
