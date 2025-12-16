namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel

open System.Collections
open System.Collections.Generic

type QLabProcess(node : LDNode) as this = 

   inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)
    do 
        if LDLabProcess.validate(node, context = context) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.DeepCopyPropertiesTo(this)

    static member input (labProcess : QLabProcess) =
        LDLabProcess.getObjects(labProcess, context = context)
        |> Seq.exactlyOne
        |> IONode

    static member output (labProcess : QLabProcess) =
        LDLabProcess.getResults(labProcess, context = context)
        |> Seq.exactlyOne
        |> IONode

    static member inputName (labProcess : QLabProcess) =
        let input = QLabProcess.input labProcess
        LDSample.getNameAsString(input, context = context)

    static member outputName (labProcess : QLabProcess) =
        let output = QLabProcess.output labProcess
        LDSample.getNameAsString(output, context = context)

    static member inputType (labProcess : QLabProcess)  =
        let input = QLabProcess.input labProcess
        input.SchemaType

    static member outputType (labProcess : QLabProcess) =
        let output = QLabProcess.output labProcess
        output.SchemaType

    member this.Input = QLabProcess.input this

    member this.Output = QLabProcess.output this

    member this.InputName = QLabProcess.inputName this

    member this.OutputName = QLabProcess.outputName this

    member this.InputType = QLabProcess.inputType this

    member this.OutputType = QLabProcess.outputType this

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
        this.Input.ObjectOf()

    member this.GetPreviousProcesses() =
        this.Output.ResultOf()

    member this.Values = 
        this.Input.Characteristics
        |> ResizeArray.append this.Output.Characteristics
        |> ResizeArray.append this.ParameterValues