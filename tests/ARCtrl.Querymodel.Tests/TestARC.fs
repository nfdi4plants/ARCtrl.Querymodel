module TestARC.Tests

open Fable.Pyxpecto
open ARCtrl
open ARCtrl.Process
open ARCtrl.QueryModel
let testArcPath = __SOURCE_DIRECTORY__ + @"\TestObjects\TestArc"
let testArc = ARC.loadAsync(testArcPath)

open ARCtrl.CrossAsync


let testCaseCrossAsync (text : string) (ca : CrossAsync<unit>) =
    ca
    |> catchWith (fun exn -> failwithf "%s" exn.Message)
    |> asAsync
    |> testCaseAsync text

let ArcTables_getNodes =
    
    testList "ARCTables_GetNodes" [
        testCaseCrossAsync "LastData" <| crossAsync {
            let! testArc = testArc
            let nodes = testArc.ArcTables.LastData
            let nodeNames = nodes |> List.map (fun n -> n.Name)
            let expected = ["sampleOutCold.txt"; "sampleOutHeat.txt"]
            Suspect.sequenceEqual nodeNames expected "LastData of full sequence"
        }
        testCaseCrossAsync "LastSamples" <| crossAsync {
            let! testArc = testArc
            let nodes = testArc.ArcTables.LastSamples
            let nodeNames = nodes |> List.map (fun n -> n.Name)
            let expected = ["CC1_prep"; "CC2_prep"; "CC3_prep"; "Co1_prep"; "Co2_prep"; "Co3_prep"; "C1_prep"; "C2_prep"; "C3_prep"; "H1_prep"; "H2_prep"; "H3_prep"]
            Suspect.sequenceEqual expected nodeNames "LastSamples of full sequence"                 
        }
        testCaseCrossAsync "LastNodes" <| crossAsync {
            let! testArc = testArc
            let nodes = testArc.ArcTables.LastNodes
            let nodeNames = nodes |> Seq.map (fun n -> n.Name)
            let expected = ["sampleOutCold.txt"; "sampleOutHeat.txt"]
            Suspect.sequenceEqual nodeNames expected "Last Nodes of full sequence"    
        }
        //testCase "RawData" (fun () ->
        //    let nodes = isa.ArcTables.RawData
        //    let nodeNames = nodes |> Seq.map (fun n -> n.Name)        
        //    let expected = ["CC1_measured";"CC2_measured";"CC3_measured";"Co1_measured";"Co2_measured";"Co3_measured";"C1_measured";"C2_measured";"C3_measured";"H1_measured";"H2_measured";"H3_measured"]
        //    Expect.sequenceEqual nodeNames expected "RawData of full sequence"    
        //)
        //testCase "LastRawData" (fun () ->
        //    let nodes = isa.ArcTables.LastRawData
        //    let nodeNames = nodes |> Seq.map (fun n -> n.Name)        
        //    let expected = ["CC1_measured";"CC2_measured";"CC3_measured";"Co1_measured";"Co2_measured";"Co3_measured";"C1_measured";"C2_measured";"C3_measured";"H1_measured";"H2_measured";"H3_measured"]
        //    Expect.sequenceEqual nodeNames expected "RawData of full sequence"    
        //)
        //testCase "FirstRawData" (fun () ->
        //    let nodes = isa.ArcTables.FirstRawData
        //    let nodeNames = nodes |> Seq.map (fun n -> n.Name)        
        //    let expected = ["CC1_measured";"CC2_measured";"CC3_measured";"Co1_measured";"Co2_measured";"Co3_measured";"C1_measured";"C2_measured";"C3_measured";"H1_measured";"H2_measured";"H3_measured"]
        //    Expect.sequenceEqual nodeNames expected "RawData of full sequence"    
        //)
    ]

let Assay_getNodes =
    testList "Assay_GetNodes" [
        
        testCaseCrossAsync "LastNodes" <| crossAsync {
            let! testArc = testArc
            let nodes = testArc.GetAssay("MSEval_Heat").LastNodes
            let nodeNames = nodes |> Seq.map (fun n -> n.Name)
            let expected = ["sampleOutHeat.txt"]
            Suspect.sequenceEqual nodeNames expected "LastData of full sequence"    
        }
    ]
let Assay_ValuesOf =
    testList "Assay_ValuesOf" [
        
        testCaseCrossAsync "ValuesOfOutput_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let values = testArc.GetAssay("MSEval_Heat").ValuesOf("sampleOutHeat.txt").WithName("Column")
            let valueValues = values |> Seq.map (fun n -> n.ValueText)
            let expected = ["C1 Intensity";"C2 Intensity";"C3 Intensity";"H1 Intensity";"H2 Intensity";"H3 Intensity"]
            Suspect.sequenceEqual valueValues expected "Did not return all values correctly"    
        }
        testCaseCrossAsync "SucceedingValuesOfInput_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let values = testArc.GetAssay("MSEval_Heat").SucceedingValuesOf("C2_measured").WithName("Column")
            let valueValues = values |> Seq.map (fun n -> n.ValueText)
            let expected = ["C2 Intensity"]
            Suspect.sequenceEqual valueValues expected "Did not return the single value correctly"    
        }
        testCaseCrossAsync "PreviousValuesOfInput_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let values = testArc.GetAssay("MSEval_Heat").PreviousValuesOf("C2_measured").WithName("Column")
            let valueValues = values |> Seq.map (fun n -> n.ValueText)
            let expected = []
            Suspect.sequenceEqual valueValues expected "Should return no values"    
        }
        testCaseCrossAsync "ValuesOfInput_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let values = testArc.GetAssay("MSEval_Heat").ValuesOf("C2_measured").WithName("Column")
            let valueValues = values |> Seq.map (fun n -> n.ValueText)
            let expected = ["C2 Intensity"]
            Suspect.sequenceEqual valueValues expected "Did not return the single value correctly"    
        }

    ]

let ArcTables_ValueOf =
    testList "ArcTable_Values" [
        testCaseCrossAsync "ValuesOf_SpecificTable" <| crossAsync {
            let! testArc = testArc
            let nodeName = "sampleOutHeat.txt"
            let protocolName =  "MS_Heat"            
            let values = testArc.ArcTables.ValuesOf(nodeName,protocolName)
            let expectedTechRep =
                ISAValue.Parameter (
                        ProcessParameterValue.create(
                            ProtocolParameter.fromString("technical replicate","MS","MS:1001808"), 
                            Value.Ontology (OntologyAnnotation("1"))
                        )
                    )
            let expectedInjVol =
                ISAValue.Parameter (
                        ProcessParameterValue.create(
                            ProtocolParameter.fromString("injection volume setting","AFR","AFR:0001577"), 
                            Value.Int 20,
                            OntologyAnnotation("microliter","UO","http://purl.obolibrary.org/obo/UO_0000101")
                        )
                    )
            let expected = 
                [
                    expectedTechRep;expectedInjVol
                    expectedTechRep;expectedInjVol
                    expectedTechRep;expectedInjVol
                    expectedTechRep;expectedInjVol
                    expectedTechRep;expectedInjVol
                    expectedTechRep;expectedInjVol
                ]
            Suspect.sequenceEqual values expected "Did not return correct values for specific table"
        }
        testCaseCrossAsync "ValuesOf" <| crossAsync {
            let! testArc = testArc
            let nodeName = "sampleOutHeat.txt"

            let valueHeaders = 
                testArc.ArcTables.ValuesOf(nodeName).DistinctHeaderCategories()
                |> Seq.map (fun x -> x.NameText)
            let expected = 
                ["biological replicate";"organism";"temperature day";"pH";"technical replicate"; "injection volume setting";"analysis software";"Column"]
            Suspect.sequenceEqual valueHeaders expected "Did not return correct values for all table"
        }
        testCaseCrossAsync "GetSpecificValue" <| crossAsync {
            let! testArc = testArc
            let rep1 = testArc.ArcTables.ValuesOf("C1_measured").WithName("biological replicate").First.ValueText
            Expect.equal rep1 "1" "Did not return correct value for specific table"
            let rep2 = testArc.ArcTables.ValuesOf("C2_measured").WithName("biological replicate").First.ValueText
            Expect.equal rep2 "2" "Did not return correct value for specific table"
        }
        testCaseCrossAsync "ValuesOf_SpecificTable_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let vals = testArc.ArcTables.ValuesOf("sampleOutHeat.txt","Growth_Heat").WithName("biological replicate").Values |> List.map (fun v -> v.ValueText)         
            Suspect.sequenceEqual vals ["1";"2";"3";"1";"2";"3"] "Did not return correct values"
        }
        testCaseCrossAsync "SpecificValue_SpecificTable_PooledOutput" <| crossAsync {
            let! testArc = testArc
            let vals = testArc.ArcTables.ValuesOf("C2_prep","Growth_Heat").WithName("biological replicate").First.ValueText
            Expect.equal vals "2" "Did not return correct value"
        }
    ]

let main = testList "TestArcTests" [
    ArcTables_getNodes
    Assay_getNodes
    Assay_ValuesOf
    ArcTables_ValueOf
]