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

    static member input (labProcess : LDNode) =
        LDLabProcess.getObjects(labProcess, context = context)
        |> Seq.exactlyOne
        |> IONode

    static member output (labProcess : LDNode) =
        LDLabProcess.getResults(labProcess, context = context)
        |> Seq.exactlyOne
        |> IONode

    static member inputName (labProcess : LDNode) =
        let input = QLabProcess.input labProcess
        LDSample.getNameAsString(input, context = context)

    static member outputName (labProcess : LDNode) =
        let output = QLabProcess.output labProcess
        LDSample.getNameAsString(output, context = context)

    static member inputType (labProcess : LDNode)  =
        let input = QLabProcess.input labProcess
        input.SchemaType

    static member outputType (labProcess : LDNode) =
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

    //member this.Values = 
    //    this.Cells
    //    |> Seq.choose (fun (header,cell) ->
    //        ISAValue.tryCompose header cell                                       
    //    )