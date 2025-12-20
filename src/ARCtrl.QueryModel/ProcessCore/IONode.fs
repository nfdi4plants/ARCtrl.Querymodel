namespace ARCtrl.QueryModel.ProcessCore

open Fable.Core
open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel

[<AttachMembers>]
type IONode(node : LDNode) as this =

    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    do         
        node.DeepCopyPropertiesTo(this)

    member this.Name = LDSample.getNameAsString(this, context = context)

    member this.IsSample = LDSample.validateSample(this, context = context)

    member this.IsSource = LDSample.validateSource(this, context = context)

    member this.IsMaterial = LDSample.validateMaterial(this, context = context)

    member this.IsFile = LDFile.validate(this, context = context)

    member this.AdditionalProperties = 
        LDSample.getAdditionalProperties(this, context = context, graph = graph)
        |> ResizeArray.map (fun apNode -> QPropertyValue(apNode))

    member this.Characteristics = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsCharacteristic)

    member this.Factors = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsFactor)

    member this.HasResultOf() = 
       this.HasProperty(resultOf, context = context)

    member this.HasObjectOf() =
        this.HasProperty(objectOf, context = context)

    member this.ResultOf() = 
        this.GetPropertyNodes(resultOf, graph = graph, context = context)

    member this.ObjectOf() =
        this.GetPropertyNodes(objectOf, graph = graph, context = context)
        
