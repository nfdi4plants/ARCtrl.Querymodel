namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel
open Fable.Core

open System.Collections
open System.Collections.Generic

/// Contains queryable ISAValues (Parameters, Factors, Characteristics)
[<AttachMembers>]
type QValueCollection(values : ResizeArray<QPropertyValue>) =

    //do values.[0].C

    member this.Item(i : int)  = values.[i]

    /// Returns the nth Item in the collection
    member this.GetAt(i : int)  = values.[i]

    /// Returns an Item in the collection with the given header name
    member this.GetByName(category : string) =
        values 
        |> Seq.pick (fun v -> if v.Category.NameText = category then Some v else None)

    /// Returns an Item in the collection with the given header category
    member this.GetByCategory(category : OntologyAnnotation) = 
        values 
        |> Seq.pick (fun v -> if v.Category = category then Some v else None)

    ///// Returns an Item in the collection whichs header category is a child of the given parent category
    //member this.ItemWithParent(parentCategory : OntologyAnnotation) = 
    //    values 
    //    |> Seq.pick (fun v -> if v.Category.IsChildTermOf(parentCategory) then Some v else None)

    /// Returns the nth Item in the collection if it exists, else returns None
    member this.TryGetAt(i : int)  = if values.Count > i then Some values.[i] else None

    /// Returns an Item in the collection with the given header name, else returns None
    member this.TryGetByName(category : string) = 
        values
        |> Seq.tryPick (fun v -> if v.Category.NameText = category then Some v else None)

    /// Returns an Item in the collection with the given header category, else returns None
    member this.TryGetByCategory(category : OntologyAnnotation) = 
        values 
        |> Seq.tryPick (fun v -> if v.Category = category then Some v else None)

    ///// Returns an Item in the collection whichs header category is a child of the given parent category, else returns None
    //member this.TryItemWithParent(parentCategory : OntologyAnnotation) = 
    //    values 
    //    |> Seq.tryPick (fun v -> if v.Category.IsChildTermOf(parentCategory) then Some v else None)

    /// Get the values as list
    member this.Values = values

    /// Return a new QValueCollection with only the characteristic values
    member this.Characteristics(?Name) = 
        values
        |> ResizeArray.filter (fun v -> 
            match Name with 
            | Some name -> 
                v.IsCharacteristic && v.NameText = name
            | None -> 
                v.IsCharacteristic
        )
        |> QValueCollection

    /// Return a new QValueCollection with only the parameter values
    member this.Parameters(?Name) = 
        values
        |> ResizeArray.filter (fun v -> 
            match Name with 
            | Some name -> 
                v.IsParameter && v.NameText = name
            | None -> 
                v.IsParameter
        )
        |> QValueCollection

    /// Return a new QValueCollection with only the factor values
    member this.Factors(?Name) = 
        values
        |> ResizeArray.filter (fun v -> 
            match Name with 
            | Some name -> 
                v.IsFactor && v.NameText = name
            | None -> 
                v.IsFactor
        )
        |> QValueCollection

    /// Return a new QValueCollection with only the factor values
    member this.Components(?Name) = 
        values
        |> ResizeArray.filter (fun v -> 
            match Name with 
            | Some name -> 
                v.IsComponent && v.NameText = name
            | None -> 
                v.IsComponent
        )
        |> QValueCollection

    /// Return a new QValueCollection with only those values, for which the predicate applied on the header return true
    member this.Filter(predicate : OntologyAnnotation -> bool) = values |> ResizeArray.filter (fun v -> predicate v.Category) |> QValueCollection

    /// Return a new QValueCollection with only those values, whichs header equals the given string
    member this.WithName(name : string) = 
        this.Filter (fun v -> v.NameText = name)

    /// Return a new QValueCollection with only those values, whichs header equals the given category
    member this.WithCategory(category : OntologyAnnotation) = 
        this.Filter((=) category)

    ///// Return a new QValueCollection with only those values, whichs header equals the given category or an equivalent category
    /////
    ///// Equivalency is deduced from XRef relationships in the given Ontology
    //member this.WithEquivalentCategory(equivalentCategory : OntologyAnnotation, ont : OboOntology) = 
    //    this.Filter (fun v -> v.IsEquivalentTo(equivalentCategory, ont))

    ///// Return a new QValueCollection with only those values, whichs header equals the given category or its child categories
    /////
    ///// Equivalency is deduced from isA relationships in the SwateAPI
    //member this.WithChildCategory(childCategory : OntologyAnnotation) = 
    //    this.Filter (fun v -> childCategory.IsChildTermOf(v))

    ///// Return a new QValueCollection with only those values, whichs header equals the given category or its child categories
    /////
    ///// Equivalency is deduced from isA relationships in the given Ontology
    //member this.WithChildCategory(childCategory : OntologyAnnotation, ont : OboOntology) = 
    //    this.Filter (fun v -> childCategory.IsChildTermOf(v, ont))

    ///// Return a new QValueCollection with only those values, whichs header equals the given category or its parent categories
    /////
    ///// Equivalency is deduced from isA relationships in the SwateAPI
    //member this.WithParentCategory(parentCategory : OntologyAnnotation) = 
    //    this.Filter (fun v -> v.IsChildTermOf(parentCategory))

    ///// Return a new QValueCollection with only those values, whichs header equals the given category or its parent categories
    /////
    ///// Equivalency is deduced from isA relationships in the given Ontology
    //member this.WithParentCategory(parentCategory : OntologyAnnotation, ont : OboOntology) = 
    //    this.Filter (fun v -> v.IsChildTermOf(parentCategory,ont))

    /// Returns a new QValueCollection that contains no duplicate entries. 
    member this.Distinct() =
        values
        |> ResizeArray.distinct
        |> QValueCollection

    /// Returns a new QValueCollection that contains no two entries with the same header Category
    member this.DistinctHeaderCategories() =
        values
        |> ResizeArray.distinctBy (fun v -> v.Category)
        |> QValueCollection

    ///// Returns true, if the QValueCollection contains a values, whichs header equals the given category or its child categories
    /////
    ///// Equivalency is deduced from isA relationships in the SwateAPI
    //member this.ContainsChildOf(parentCategory : OntologyAnnotation) =
    //    values
    //    |> Seq.exists (fun v -> v.Category.IsChildTermOf(parentCategory))

    /// Returns true, if the QValueCollection contains a values, whichs header equals the given category
    member this.ContainsByCategory(category : OntologyAnnotation) =
        values
        |> Seq.exists (fun v -> v.Category = category)

    /// Returns true, if the QValueCollection contains a values, whichs headername equals the given category
    member this.ContainsByName(name : string) =
        values
        |> Seq.exists (fun v -> v.NameText = name)

    interface IEnumerable<QPropertyValue> with
        member this.GetEnumerator() = (values).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() = (this :> IEnumerable<QPropertyValue>).GetEnumerator() :> IEnumerator

    static member (@) (ps1 : QValueCollection,ps2 : QValueCollection) = ResizeArray.append ps1.Values ps2.Values |> QValueCollection


    /// Return the number of values in the collection
    member this.IsEmpty = this.Values.Count = 0

    /// Return the number of values in the collection
    member this.Length = this.Values.Count

    /// Return first ISAValue in collection
    member this.First = this.Values.[0]

    /// Return first ISAValue in collection if it exists, else returns None
    member this.TryFirst = if this.IsEmpty then None else Some this.First

    /// Return first ISAValue in collection
    member this.Last = this.Values.[this.Length - 1]

/// Contains queryable ISAValues (Parameters, Factors, Characteristics)
[<AttachMembers>]
type IOQValueCollection(values : KeyValuePair<string*string,QPropertyValue> ResizeArray) =

    /// Returns the nth Item in the collection
    member this.GetAt(i : int)  = values.[i]

    member this.GetByName(name : string) = values |> Seq.pick (fun kv -> if kv.Value.NameText = name then Some kv.Key else None)

    member this.GetByIOName(ioKey : string*string) = values |> Seq.pick (fun kv -> if ioKey = kv.Key then Some kv.Value else None)

    member this.GetByCategory(category : OntologyAnnotation) = values |> Seq.pick (fun kv -> if kv.Value.Category = category then Some kv.Key else None)

    member this.WithInput(inp : string) = 
        values 
        |> ResizeArray.choose (fun kv -> if (fst kv.Key) = inp then Some kv.Value else None)
        |> QValueCollection

    member this.WithOutput(inp : string) = 
        values |> ResizeArray.choose (fun kv -> if (snd kv.Key) = inp then Some kv.Value else None)
        |> QValueCollection

    member this.Values(?Name) = 
        values 
        |> ResizeArray.choose (fun kv -> 
            match Name with
            | Some name -> 
                if kv.Value.NameText = name then Some kv.Value
                else None
            | None -> Some kv.Value
        )
        |> QValueCollection

    member this.Characteristics(?Name) = 
        values
        |> ResizeArray.filter (fun kv -> 
            match Name with 
            | Some name -> 
                kv.Value.IsCharacteristic && kv.Value.NameText = name
            | None -> 
                kv.Value.IsCharacteristic
        )
        |> IOQValueCollection

    member this.Parameters(?Name) = 
        values
        |> ResizeArray.filter (fun kv -> 
            match Name with 
            | Some name -> 
                kv.Value.IsParameter && kv.Value.NameText = name
            | None -> 
                kv.Value.IsParameter
        )
        |> IOQValueCollection

    member this.Factors(?Name) = 
        values
        |> ResizeArray.filter (fun kv -> 
            match Name with 
            | Some name -> 
                kv.Value.IsFactor && kv.Value.NameText = name
            | None -> 
                kv.Value.IsFactor
        )
        |> IOQValueCollection

    member this.Components(?Name) = 
        values
        |> ResizeArray.filter (fun kv -> 
            match Name with 
            | Some name -> 
                kv.Value.IsComponent && kv.Value.NameText = name
            | None -> 
                kv.Value.IsComponent
        )
        |> IOQValueCollection

    member this.WithCategory(category : OntologyAnnotation) = 
        values
        |> ResizeArray.filter (fun kv -> kv.Value.Category = category)
        |> IOQValueCollection

    member this.WithName(name : string) = 
        values
        |> ResizeArray.filter (fun kv -> kv.Value.Category.NameText = name)
        |> IOQValueCollection

    member this.GroupBySource =
        values
        |> Seq.groupBy (fun kv -> fst kv.Key)
        |> Seq.map (fun (source,vals) -> source, vals |> Seq.map (fun kv -> snd kv.Key,kv.Value))

    member this.GroupBySink =
        values
        |> Seq.groupBy (fun kv -> snd kv.Key)
               |> Seq.map (fun (sink,vals) -> sink, vals |> Seq.map (fun kv -> fst kv.Key,kv.Value))
    
    interface IEnumerable<KeyValuePair<string*string,QPropertyValue>> with
        member this.GetEnumerator() = (values).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() = (this :> IEnumerable<KeyValuePair<string*string,QPropertyValue>>).GetEnumerator() :> IEnumerator

[<AutoOpen>]
module IOQValueCollectionExtensions =

    type IOQValueCollection with

        /// Return the number of values in the collection
        member this.Length = this.Values().Length

        /// Return first ISAValue in collection
        member this.First = this.Values().First

        /// Return first ISAValue in collection
        member this.Last = this.Values().Last