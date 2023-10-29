﻿namespace ARCtrl.QueryModel

open ARCtrl.ISA
open OntologyAnnotation
open System.Text.Json.Serialization



/// 
type KVElement =
    {Header : CompositeHeader; Value : CompositeCell}


[<AutoOpen>]
module KVElementExtensions = 

    type KVElement with

        /// Returns true, if the value is a characteristic value
        member this.IsCharacteristicValue =
            this.Header.isCharacteristic

        /// Returns true, if the value is a parameter value
        member this.IsParameterValue =
            this.Header.isParameter

        /// Returns true, if the value is a factor value
        member this.IsFactorValue =
            this.Header.isFactor
            
        /// Returns true, if the value is a characteristic value
        member this.IsComponent =
            this.Header.isComponent

        /// Returns the ontology of the category of the KVElement
        member this.Category =
            this.Header

        /// Returns the ontology of the unit of the KVElement
        member this.Unit =
            this.Value

        /// Returns the ontology of the unit of the KVElement
        member this.TryUnit =
            match this with
            | Parameter p       -> p.Unit       
            | Characteristic c  -> c.Unit       
            | Factor f          -> f.Unit         
            | Component c       -> c.ComponentUnit

        /// Returns the value of the KVElement
        member this.Value =
            match this with
            | Parameter p       -> try p.Value.Value            with | _ -> failwith $"Parameter {p.NameText} does not contain value"
            | Characteristic c  -> try c.Value.Value            with | _ -> failwith $"Characteristic {c.NameText} does not contain value"
            | Factor f          -> try f.Value.Value            with | _ -> failwith $"Factor {f.NameText} does not contain value"
            | Component c       -> try c.ComponentValue.Value   with | _ -> failwith $"Component {c.NameText} does not contain value"

        /// Returns the value of the KVElement
        member this.TryValue =
            match this with
            | Parameter p       -> try Some p.Value.Value           with | _ -> None
            | Characteristic c  -> try Some c.Value.Value           with | _ -> None
            | Factor f          -> try Some f.Value.Value           with | _ -> None
            | Component c       -> try Some c.ComponentValue.Value  with | _ -> None

        /// Returns true, if the KVElement has a unit
        member this.HasUnit =
            match this with
            | Parameter p       -> p.Unit.IsSome
            | Characteristic c  -> c.Unit.IsSome
            | Factor f          -> f.Unit.IsSome
            | Component c       -> c.ComponentUnit.IsSome

        /// Returns true, if the KVElement has a value
        member this.HasValue =
            match this with
            | Parameter p       -> p.Value.IsSome
            | Characteristic c  -> c.Value.IsSome
            | Factor f          -> f.Value.IsSome
            | Component c       -> c.ComponentValue.IsSome

        /// Returns true, if the KVElement has a category
        member this.HasCategory = 
            match this with
            | Parameter p       -> p.Category.IsSome
            | Characteristic c  -> c.Category.IsSome
            | Factor f          -> f.Category.IsSome
            | Component c       -> c.ComponentType.IsSome

        /// Returns the header of the Value as string
        member this.HeaderText = 
            match this with
            | Parameter p       -> $"Parameter [{this.NameText}]"       
            | Characteristic c  -> $"Characteristic [{this.NameText}]" 
            | Factor f          -> $"Factor [{this.NameText}]"          
            | Component c       -> $"Component [{this.NameText}]" 

        /// Returns the header of the Value as string if it exists, else returns None
        member this.TryHeaderText = 
            match this with
            | Parameter p       -> if this.HasCategory then Some $"Parameter [{this.NameText}]"         else None
            | Characteristic c  -> if this.HasCategory then Some $"Characteristic [{this.NameText}]"    else None
            | Factor f          -> if this.HasCategory then Some $"Factor [{this.NameText}]"            else None
            | Component c       -> if this.HasCategory then Some $"Component [{this.NameText}]"         else None

        /// Returns the name of the Value as string
        member this.NameText = this.Category.NameText
  
        /// Returns the name of the Value as string if it exists, else returns None
        member this.TryNameText = 
            this.TryCategory |> Option.map (fun c -> c.NameText)

        /// Returns the unit of the Value as string
        member this.UnitText = this.Unit.NameText

        /// Returns the unit of the Value as string if it exists, else returns None
        member this.TryUnitText = 
            this.TryUnit |> Option.map (fun u -> u.NameText)

        /// Returns the value of the Value as string
        member this.ValueText = this.Value.AsName()

        /// Returns the value of the Value as string if it exists, else returns None
        member this.TryValueText = 
            this.TryValue |> Option.map (fun v -> v.AsName())

        /// Returns the value and unit of the Value as string
        member this.ValueWithUnitText =
            match this with
            | Parameter p       -> p.ValueWithUnitText
            | Characteristic c  -> c.ValueWithUnitText
            | Factor f          -> f.ValueWithUnitText
            | Component c       -> c.ValueWithUnitText

        /// Returns the value and unit of the Value as string if it exists, else returns None
        member this.TryValueWithUnitText =
            match this with
            | Parameter p       -> if this.HasValue && this.HasUnit then Some p.ValueWithUnitText else None
            | Characteristic c  -> if this.HasValue && this.HasUnit then Some c.ValueWithUnitText else None
            | Factor f          -> if this.HasValue && this.HasUnit then Some f.ValueWithUnitText else None
            | Component c       -> if this.HasValue && this.HasUnit then Some c.ValueWithUnitText else None

        member this.HasParentCategory(parentOntology : OntologyAnnotation, ont : Obo.OboOntology) = 
            match this.TryCategory with
            | Some oa -> oa.IsChildTermOf(parentOntology,ont)
            | None -> false
            
        member this.HasParentCategory(parentOntology : OntologyAnnotation) = 
            match this.TryCategory with
            | Some oa -> oa.IsChildTermOf(parentOntology)
            | None -> false

        member this.GetAs(targetOntology : string, ont : Obo.OboOntology) = 
            match this with
            | Parameter p       -> p.GetAs(targetOntology,ont) |> Parameter
            | Characteristic c  -> c.GetAs(targetOntology,ont) |> Characteristic
            | Factor f          -> f.GetAs(targetOntology,ont) |> Factor
            | Component c       -> c.GetAs(targetOntology,ont) |> Component

        member this.TryGetAs(targetOntology : string, ont : Obo.OboOntology) = 
            match this with
            | Parameter p       -> p.TryGetAs(targetOntology,ont) |> Option.map Parameter
            | Characteristic c  -> c.TryGetAs(targetOntology,ont) |> Option.map Characteristic
            | Factor f          -> f.TryGetAs(targetOntology,ont) |> Option.map Factor
            | Component c       -> c.TryGetAs(targetOntology,ont) |> Option.map Component