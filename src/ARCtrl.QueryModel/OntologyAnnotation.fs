namespace ARCtrl.QueryModel

//open ARCtrl
//open Swate.Api
//open ARCtrl.Process

//[<AutoOpen>]
//module OntologyAnnotation =

//    type OntologyAnnotation with
       

//        /// Translates a SwateAPI `term` into an ISADotNet `OntologyAnnotation`
//        static member ofTerm (term : Term) =
//            let description = Comment("Description",term.Definition)
//            let oa = OntologyAnnotation(term.Name,tan = term.Accession,comments = ResizeArray [|description|])
//            oa.TermSourceREF <- oa.TANInfo |> Option.map (fun t -> t.IDSpace)
//            oa

//        /// Translates an ISADotNet `OntologyAnnotation` into a SwateAPI `term`
//        static member toTerm (term : OntologyAnnotation) =
//            TermMinimal.create term.NameText term.TermAccessionShort

        ///// Translates a OBO `term` into an ISADotNet `OntologyAnnotation`
        //static member ofOboTerm (term : OboTerm) =
        //    OntologyAnnotation.fromString(term.Name, tan = term.Id)
        //    |> fun o -> {o with TermSourceREF = o.TANInfo |> Option.map (fun t -> t.IDSpace)}

        ///// Translates an ISADotNet `OntologyAnnotation` into a OBO `term`
        //static member toOboTerm (term : OntologyAnnotation) =
        //    OboTerm.Create(term.TermAccessionShort,term.NameText)

        //member this.ToOboTerm() =
        //    OntologyAnnotation.toOboTerm this

        //member this.ToTerm() =
        //    OntologyAnnotation.toTerm(this)

        //static member findTerm(nameOrId : string) =
        //    Term.Search(nameOrId, 1).[0]

        //static member findTerm(nameOrId : string,ont : OboOntology) =
        //    match ont.TryGetOntologyAnnotation nameOrId with
        //    | Some oa ->
        //        oa
        //    | None ->
        //        match ont.TryGetOntologyAnnotationByName nameOrId with
        //        | Some oa -> oa
        //        | None -> failwithf "could not find Ontology term %s in given ontology" nameOrId

        //static member isChildTerm(parent : OntologyAnnotation,child : OntologyAnnotation,ont : OboOntology) =
        //    ont.GetParentOntologyAnnotations(child)
        //    |> List.contains parent

        //member this.IsChildTermOf(parent : OntologyAnnotation) =
        //    OntologyAnnotation.isChildTerm(parent,this)

        //static member isChildTerm (parent : OntologyAnnotation,child : OntologyAnnotation) =
        //    Term.SearchByParent(child.NameText, 1, parent |> OntologyAnnotation.toTerm)
        //    |> Array.isEmpty
        //    |> not       

        //member this.IsChildTermOf(parent : OntologyAnnotation, ont : OboOntology) =
        //    OntologyAnnotation.isChildTerm(parent,this,ont)

        //static member isEquivalentTo(term : OntologyAnnotation,targetTerm : OntologyAnnotation,ont : OboOntology) =
        //    ont.GetEquivalentOntologyAnnotations(term.TermAccessionShort)
        //    |> List.contains targetTerm

        //member this.IsEquivalentTo(targetTerm : OntologyAnnotation, ont : OboOntology) =
        //    OntologyAnnotation.isEquivalentTo(targetTerm,this,ont)

        //static member getAs (term : OntologyAnnotation, targetOntology : string, ont : OboOntology) =
        //    ont.GetEquivalentOntologyAnnotations(term)
        //    |> List.find (fun t -> t.TermSourceREFString = targetOntology)

        //member this.GetAs(targetOntology : string, ont : OboOntology) =
        //    OntologyAnnotation.getAs(this,targetOntology,ont)

        //static member tryGetAs (term : OntologyAnnotation, targetOntology : string, ont : OboOntology) =
        //    ont.GetEquivalentOntologyAnnotations(term)
        //    |> List.tryFind (fun t -> t.TermSourceREFString = targetOntology)

        //member this.TryGetAs(targetOntology : string, ont : OboOntology) =
        //    OntologyAnnotation.tryGetAs(this,targetOntology,ont)

    //type Protocol with
        
    //    static member isChildProtocolTypeOf(protocol : Protocol,parent : OntologyAnnotation) =
    //        protocol.ProtocolType
    //        |> Option.map (fun t -> t.IsChildTermOf(parent))
    //        |> Option.defaultValue false

        //static member isChildProtocolTypeOf(protocol : Protocol,parent : OntologyAnnotation, ont : OboOntology) =
        //    protocol.ProtocolType
        //    |> Option.map (fun t -> t.IsChildTermOf(parent,ont))
        //    |> Option.defaultValue false

        //member this.IsChildProtocolTypeOf(parent : OntologyAnnotation) =
        //    Protocol.isChildProtocolTypeOf(this,parent)

        //member this.IsChildProtocolTypeOf(parent : OntologyAnnotation, ont : OboOntology) =
        //    Protocol.isChildProtocolTypeOf(this,parent,ont)

    //type Value with
    
        //member this.GetAs(targetOntology : string, ont : OboOntology) =
        //    match this with
        //    | Ontology oa -> Ontology (oa.GetAs(targetOntology, ont))
        //    | _ -> this

        //member this.TryGetAs(targetOntology : string, ont : OboOntology) =
        //    match this with
        //    | Ontology oa -> 
        //        oa.TryGetAs(targetOntology, ont)
        //        |> Option.map Ontology
        //    | _ -> None

    //type ProcessParameterValue with
    
    //    member this.GetAs(targetOntology : string, ont : OboOntology) =
    //        {this with Value = this.Value |> Option.map (fun v -> v.GetAs(targetOntology,ont))}

    //    member this.TryGetAs(targetOntology : string, ont : OboOntology) =
    //        this.Value
    //        |> Option.bind (fun v -> v.TryGetAs(targetOntology,ont))
    //        |> Option.map (fun v -> {this with Value = Some v})

    //type MaterialAttributeValue with
    
    //    member this.GetAs(targetOntology : string, ont : OboOntology) =
    //        {this with Value = this.Value |> Option.map (fun v -> v.GetAs(targetOntology,ont))}

    //    member this.TryGetAs(targetOntology : string, ont : OboOntology) =
    //        this.Value
    //        |> Option.bind (fun v -> v.TryGetAs(targetOntology,ont))
    //        |> Option.map (fun v -> {this with Value = Some v})

    //type FactorValue with
    
    //    member this.GetAs(targetOntology : string, ont : OboOntology) =
    //        {this with Value = this.Value |> Option.map (fun v -> v.GetAs(targetOntology,ont))}

    //    member this.TryGetAs(targetOntology : string, ont : OboOntology) =
    //        this.Value
    //        |> Option.bind (fun v -> v.TryGetAs(targetOntology,ont))
    //        |> Option.map (fun v -> {this with Value = Some v})

    //type Component with
        
    //    member this.GetAs(targetOntology : string, ont : OboOntology) =
    //        {this with ComponentValue = this.ComponentValue |> Option.map (fun v -> v.GetAs(targetOntology,ont))}

    //    member this.TryGetAs(targetOntology : string, ont : OboOntology) =
    //        this.ComponentValue
    //        |> Option.bind (fun v -> v.TryGetAs(targetOntology,ont))
    //        |> Option.map (fun v -> {this with ComponentValue = Some v})