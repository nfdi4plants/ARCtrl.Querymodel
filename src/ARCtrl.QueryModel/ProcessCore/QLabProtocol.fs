namespace ARCtrl.QueryModel.ProcessCore

open Fable.Core
open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel

open System.Collections
open System.Collections.Generic

[<AttachMembers>]
type QLabProtocol(node : LDNode) as this = 

   inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)
    do 
        if LDLabProtocol.validate(node, context = context) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.DeepCopyPropertiesTo(this)

    static member name (labProtocol : QLabProtocol) =
        LDLabProtocol.getNameAsString(labProtocol, context = context)

    static member components (labProtocol : QLabProtocol) =
        LDLabProtocol.getComponents(labProtocol, context = context)
        |> ResizeArray.map (fun cvNode -> QPropertyValue(cvNode))

    member this.Name = 
        QLabProtocol.name this

    member this.Components = 
        QLabProtocol.components this

    override this.GetHashCode () : int = 
        this.Id.GetHashCode()

    override this.Equals(obj: obj) : bool =
        match obj with
        | :? QLabProtocol as other -> this.Id = other.Id
        | _ -> false