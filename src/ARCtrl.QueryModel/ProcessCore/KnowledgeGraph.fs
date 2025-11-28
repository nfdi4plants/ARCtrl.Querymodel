namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl.ROCrate

[<AutoOpen>]
module KnowledgeGraph =

    let mutable graph = LDGraph()
    let mutable context = 
        LDContext(
            baseContexts = ResizeArray[
                Context.initBioschemasContext()
                Context.initV1_2()
            ]
        )