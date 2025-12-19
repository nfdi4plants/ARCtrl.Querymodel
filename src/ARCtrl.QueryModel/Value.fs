namespace ARCtrl.QueryModel

open ARCtrl
open ARCtrl.Process
//open OntologyAnnotation

open System.Collections.Generic
open ARCtrl.Process.Conversion

type ISAValue =
    | Parameter of ProcessParameterValue
    | Characteristic of MaterialAttributeValue
    | Factor of FactorValue
    | Component of Component

    member this.MapCategory(f : OntologyAnnotation -> OntologyAnnotation) = 
        match this with
        | Parameter         p -> p.MapCategory(f) |> Parameter
        | Characteristic    c -> c.MapCategory(f) |> Characteristic
        | Factor            c -> c.MapCategory(f) |> Factor
        | Component         c -> c.MapCategory(f) |> Component 

    static member tryCompose (header : CompositeHeader) (cell : CompositeCell) =
        if header.isCharacteristic then 
            JsonTypes.composeCharacteristicValue header cell 
            |> Characteristic 
            |> Some
        elif header.isFactor then
            JsonTypes.composeFactorValue header cell
            |> Factor
            |> Some
        elif header.isComponent then
            JsonTypes.composeComponent header cell
            |> Component
            |> Some
        elif header.isParameter then
            JsonTypes.composeParameterValue header cell
            |> Parameter
            |> Some
        else None



[<AutoOpen>]
module ISAValueExtensions = 

    type ISAValue with

        /// Returns true, if the value is a characteristic value
        member this.IsCharacteristicValue =
            match this with
            | Characteristic _  -> true
            | _                 -> false

        /// Returns true, if the value is a parameter value
        member this.IsParameterValue =
            match this with
            | Parameter _   -> true
            | _             -> false

        /// Returns true, if the value is a factor value
        member this.IsFactorValue =
            match this with
            | Factor _  -> true
            | _         -> false
            
        /// Returns true, if the value is a characteristic value
        member this.IsComponent =
            match this with
            | Component _  -> true
            | _         -> false

        /// Returns the ontology of the category of the ISAValue
        member this.Category =
            match this with
            | Parameter p       -> try p.Category.Value.ParameterName.Value         with | _ -> failwith $"Parameter does not contain category"
            | Characteristic c  -> try c.Category.Value.CharacteristicType.Value    with | _ -> failwith $"Characteristic does not contain category"
            | Factor f          -> try f.Category.Value.FactorType.Value            with | _ -> failwith $"Factor does not contain category"
            | Component c       -> try c.ComponentType.Value                        with | _ -> failwith $"Component does not contain category"

        /// Returns the ontology of the category of the ISAValue
        member this.TryCategory =
            match this with
            | Parameter p       -> p.Category |> Option.bind (fun c -> c.ParameterName)
            | Characteristic c  -> c.Category |> Option.bind (fun c -> c.CharacteristicType)
            | Factor f          -> f.Category |> Option.bind (fun c -> c.FactorType)
            | Component c       -> c.ComponentType

        /// Returns the ontology of the unit of the ISAValue
        member this.Unit =
            match this with
            | Parameter p       -> try p.Unit.Value          with | _ -> failwith $"Parameter {p.NameText} does not contain unit"
            | Characteristic c  -> try c.Unit.Value          with | _ -> failwith $"Characteristic {c.NameText} does not contain unit"
            | Factor f          -> try f.Unit.Value          with | _ -> failwith $"Factor {f.NameText} does not contain unit"
            | Component c       -> try c.ComponentUnit.Value with | _ -> failwith $"Component {c.NameText} does not contain unit"

        /// Returns the ontology of the unit of the ISAValue
        member this.TryUnit =
            match this with
            | Parameter p       -> p.Unit       
            | Characteristic c  -> c.Unit       
            | Factor f          -> f.Unit         
            | Component c       -> c.ComponentUnit

        /// Returns the value of the ISAValue
        member this.Value =
            match this with
            | Parameter p       -> try p.Value.Value            with | _ -> failwith $"Parameter {p.NameText} does not contain value"
            | Characteristic c  -> try c.Value.Value            with | _ -> failwith $"Characteristic {c.NameText} does not contain value"
            | Factor f          -> try f.Value.Value            with | _ -> failwith $"Factor {f.NameText} does not contain value"
            | Component c       -> try c.ComponentValue.Value   with | _ -> failwith $"Component {c.NameText} does not contain value"

        /// Returns the value of the ISAValue
        member this.TryValue =
            match this with
            | Parameter p       -> try Some p.Value.Value           with | _ -> None
            | Characteristic c  -> try Some c.Value.Value           with | _ -> None
            | Factor f          -> try Some f.Value.Value           with | _ -> None
            | Component c       -> try Some c.ComponentValue.Value  with | _ -> None

        /// Returns true, if the ISAValue has a unit
        member this.HasUnit =
            match this with
            | Parameter p       -> p.Unit.IsSome
            | Characteristic c  -> c.Unit.IsSome
            | Factor f          -> f.Unit.IsSome
            | Component c       -> c.ComponentUnit.IsSome

        /// Returns true, if the ISAValue has a value
        member this.HasValue =
            match this with
            | Parameter p       -> p.Value.IsSome
            | Characteristic c  -> c.Value.IsSome
            | Factor f          -> f.Value.IsSome
            | Component c       -> c.ComponentValue.IsSome

        /// Returns true, if the ISAValue has a category
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
        member this.ValueText = this.Value.Text

        /// Returns the value of the Value as string if it exists, else returns None
        member this.TryValueText = 
            this.TryValue |> Option.map (fun v -> v.Text)

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

        //member this.HasParentCategory(parentOntology : OntologyAnnotation, ont : OboOntology) = 
        //    match this.TryCategory with
        //    | Some oa -> oa.IsChildTermOf(parentOntology,ont)
        //    | None -> false
            
        //member this.HasParentCategory(parentOntology : OntologyAnnotation) = 
        //    match this.TryCategory with
        //    | Some oa -> oa.IsChildTermOf(parentOntology)
        //    | None -> false

        //member this.GetAs(targetOntology : string, ont : OboOntology) = 
        //    match this with
        //    | Parameter p       -> p.GetAs(targetOntology,ont) |> Parameter
        //    | Characteristic c  -> c.GetAs(targetOntology,ont) |> Characteristic
        //    | Factor f          -> f.GetAs(targetOntology,ont) |> Factor
        //    | Component c       -> c.GetAs(targetOntology,ont) |> Component

        //member this.TryGetAs(targetOntology : string, ont : OboOntology) = 
        //    match this with
        //    | Parameter p       -> p.TryGetAs(targetOntology,ont) |> Option.map Parameter
        //    | Characteristic c  -> c.TryGetAs(targetOntology,ont) |> Option.map Characteristic
        //    | Factor f          -> f.TryGetAs(targetOntology,ont) |> Option.map Factor
        //    | Component c       -> c.TryGetAs(targetOntology,ont) |> Option.map Component
