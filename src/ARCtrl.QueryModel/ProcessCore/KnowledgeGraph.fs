namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl.ROCrate
open ARCtrl.QueryModel

[<AutoOpen>]
module KnowledgeGraph =

    let objectOf = "objectOf"
    let resultOf = "resultOf"

    let mutable graph = LDGraph()
    let mutable context = 
        LDContext(
            baseContexts = ResizeArray[
                Context.initBioschemasContext()
                Context.initV1_2()
            ]
        )

    let indexGraph (ldGraph: LDGraph) =
        graph <- ldGraph
        ldGraph.Nodes
        |> Seq.iter (fun n ->
            if LDLabProcess.validate(n, context = context) then
                let inputs = LDLabProcess.getObjects(n, graph = graph, context = context)
                let outputs = LDLabProcess.getResults(n, graph = graph, context = context)
                inputs
                |> Seq.iter (fun inputNode ->
                    let objectOfs = inputNode.GetPropertyValues(objectOf, context = context)
                    objectOfs.Add(LDRef(n.Id))
                    objectOfs
                    |> ResizeArray.distinct
                    |> fun v -> inputNode.SetProperty(objectOf, v, context = context)  
                )
                outputs
                |> Seq.iter (fun outputNode ->
                    let resultOfs = outputNode.GetPropertyValues(resultOf, context = context)
                    resultOfs.Add(LDRef(n.Id))
                    resultOfs
                    |> ResizeArray.distinct
                    |> fun v -> outputNode.SetProperty(resultOf, v, context = context)
                )   
        )