namespace ARCtrl.QueryModel.ProcessCore

open Fable.Core
open ARCtrl
open ARCtrl.ROCrate

open ARCtrl.QueryModel
type QPropertyValue(node : LDNode) as this =

    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    do 
        if LDPropertyValue.validate(node, context = context) |> not then
            failwithf "The provided node with id %s is not a valid PropertyValue" node.Id
        node.DeepCopyPropertiesTo(this)

    member this.IsCharacteristic =
        LDPropertyValue.validateCharacteristicValue(this, context = context)


    member this.IsParameter =
        LDPropertyValue.validateParameterValue(this, context = context)

    member this.IsFactor =
        LDPropertyValue.validateFactorValue(this, context = context)

    member this.IsComponent =
        LDPropertyValue.validateComponent(this, context = context)

    member this.TryNameText =
        LDPropertyValue.tryGetNameAsString(this, context = context)


    member this.Category =
        match LDPropertyValue.tryGetPropertyIDAsString(this, context = context) with
        | Some propertyID ->
            OntologyAnnotation.fromTermAnnotation(propertyID, ?name = this.TryNameText)
        | None ->
            OntologyAnnotation(name = this.NameText)

    /// Returns the name of the Category as string
    member this.NameText =  
        LDPropertyValue.getNameAsString(this, context = context)

    member this.TryUnitText =
        LDPropertyValue.tryGetUnitTextAsString(this, context = context)

    member this.UnitText =
        LDPropertyValue.tryGetUnitTextAsString(this, context = context)
        |> Option.get

    member this.TryValueText =
        LDPropertyValue.tryGetValueAsString(this, context = context)

    member this.ValueText =
        LDPropertyValue.getValueAsString(this, context = context)

    member this.ValueWithUnitText =
        match this.TryUnitText with
        | Some unitText -> $"{this.ValueText} {unitText}"
        | None -> this.ValueText
