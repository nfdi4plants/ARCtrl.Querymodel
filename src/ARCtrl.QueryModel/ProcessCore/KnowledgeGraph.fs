namespace ARCtrl.QueryModel.ProcessCore

open ARCtrl.ROCrate
open ARCtrl.QueryModel

[<AutoOpen>]
module KnowledgeGraph =

    let objectOf = "objectOf"
    let resultOf = "resultOf"

    let context = 
        LDContext(
                baseContexts = ResizeArray[
                    Context.initBioschemasContext()
                    Context.initV1_2()
                ]
            )