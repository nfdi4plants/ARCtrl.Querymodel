namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl
open Fable.Core
open ARCtrl.ROCrate
open ARCtrl.QueryModel

open System.Collections
open System.Collections.Generic

[<AttachMembers>]
type QLabProcess(node : LDNode) as this = 

   inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)
    do 
        if LDLabProcess.validate(node, context = context) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.DeepCopyPropertiesTo(this)

    static member inputs (labProcess : QLabProcess) =
        LDLabProcess.getObjects(labProcess, graph = graph, context = context)
        |> ResizeArray.map IONode

    static member outputs (labProcess : QLabProcess) =
        LDLabProcess.getResults(labProcess, graph = graph, context = context)
        |> ResizeArray.map IONode

    static member inputNames (labProcess : QLabProcess) =
        let inputs = QLabProcess.inputs labProcess
        inputs |> ResizeArray.map (fun i -> LDSample.getNameAsString(i, context = context))

    static member outputNames (labProcess : QLabProcess) =
        let outputs = QLabProcess.outputs labProcess
        outputs |> ResizeArray.map (fun i -> LDSample.getNameAsString(i, context = context))

    static member inputTypes (labProcess : QLabProcess)  =
        let inputs = QLabProcess.inputs labProcess
        inputs |> ResizeArray.map (fun i -> i.SchemaType)

    static member outputTypes (labProcess : QLabProcess) =
        let outputs = QLabProcess.outputs labProcess
        outputs |> ResizeArray.map (fun i -> i.SchemaType)

    member this.Inputs = QLabProcess.inputs this

    member this.Outputs = QLabProcess.outputs this

    member this.InputNames = QLabProcess.inputNames this

    member this.OutputNames = QLabProcess.outputNames this

    member this.InputTypes = QLabProcess.inputTypes this

    member this.OutputTypes = QLabProcess.outputTypes this

    member this.ParameterValues =
        LDLabProcess.getParameterValues(this, context = context)
        |> ResizeArray.map (fun pvNode -> QPropertyValue(pvNode))

    member this.Protocol = 
        LDLabProcess.tryGetExecutesLabProtocol(this, context = context)
        |> Option.map (fun lpNode -> QLabProtocol(lpNode))

    member this.Components = 
        match this.Protocol with
        | Some protocolNode ->
            protocolNode.Components
        | None -> ResizeArray()

    member this.GetNextProcesses() =
        this.Inputs
        |> ResizeArray.collect (fun ioNode -> ioNode.ObjectOf())
        |> ResizeArray.distinctBy (fun p -> p.Id)

    member this.GetPreviousProcesses() =
        this.Outputs
        |> ResizeArray.collect (fun ioNode -> ioNode.ResultOf())
        |> ResizeArray.distinctBy (fun p -> p.Id)

    member this.Values = 
        (this.Inputs |> ResizeArray.collect (fun i -> i.Characteristics))
        |> ResizeArray.append (this.Outputs |> ResizeArray.collect (fun o -> o.Factors))
        |> ResizeArray.append this.ParameterValues
        |> ResizeArray.append this.Components