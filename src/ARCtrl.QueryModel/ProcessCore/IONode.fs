namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl
open ARCtrl.ROCrate

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
