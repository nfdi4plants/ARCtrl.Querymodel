module ProcessCore.Tests

open Fable.Pyxpecto
open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel.ProcessCore

let constructors =
    
    testList "constructors" [
        testCase "QLabProcess" <| fun _ ->
            let node = LDLabProcess.create(name = "Test Process", id = "#Test_Process_ID")
            let p = QLabProcess(node)
            Expect.equal p.Id "#Test_Process_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test Process" "Name property should be set correctly"
        testCase "IONode" <| fun _ ->
            let node = LDSample.create(name = "Test IO Node", id = "#Test_IO_Node_ID")
            let p = IONode(node)
            Expect.equal p.Id "#Test_IO_Node_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test IO Node" "Name property should be set correctly"
        testCase "QLabProtocol" <| fun _ ->
            let node = LDLabProtocol.create(name = "Test QLab Protocol", id = "#Test_QLab_Protocol_ID")
            let p = QLabProtocol(node)
            Expect.equal p.Id "#Test_QLab_Protocol_ID" "Id property should be set correctly"
            Expect.equal p.Name "Test QLab Protocol" "Name property should be set correctly"
        testCase "QGraph" <| fun _ -> 
            let sample = LDSample.create(name = "Test IO Node", id = "#Test_IO_Node_ID")
            let data = LDFile.create(name = "MyFile.txt")
            let proc = LDLabProcess.create(name = "Test Process", id = "#Test_Process_ID", objects = ResizeArray [sample], results = ResizeArray[data])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc])
            let graph = QGraph(dataset.Flatten())           
            Expect.hasLength graph.Nodes 4 "Graph should have 4 nodes"
            Expect.hasLength graph.ProcessSequence.Processes 1 "Graph should have 1 process in sequence"
            let procNode = graph.ProcessSequence.Processes.[0]
            Expect.equal procNode.Id "#Test_Process_ID" "Process Id should match"
            
            Expect.hasLength procNode.Inputs 1 "Process should have 1 input"     
            let input = procNode.Inputs[0]
            Expect.equal input.Id "#Test_IO_Node_ID" "Input Id should match"
            Expect.hasLength (input.ObjectOf()) 1 "Input should be object of 1 process"
            let objectOf = input.ObjectOf()[0]
            Expect.equal objectOf.Id "#Test_Process_ID" "ObjectOf process Id should match"

            Expect.hasLength procNode.Outputs 1 "Process should have 1 output"
            let output = procNode.Outputs[0]
            Expect.equal output.Id data.Id "Output Id should match"
            Expect.hasLength (output.ResultOf()) 1 "Output should be result of 1 process"
            let resultOf = output.ResultOf()[0]
            Expect.equal resultOf.Id "#Test_Process_ID" "ResultOf process Id should match"
    ]

let getNodes =
    
    testList "nodeRetrieval" [
        testList "SingleProcess" [
            let sample = LDSample.create(name = "Test IO Node", id = "#Test_IO_Node_ID")
            let data = LDFile.create(name = "MyFile.txt")
            let proc = LDLabProcess.create(name = "Test Process", id = "#Test_Process_ID", objects = ResizeArray [sample], results = ResizeArray[data])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc])
            let graph = QGraph(dataset.Flatten())
            testCase "Get IONodes" <| fun _ ->
                Expect.hasLength graph.IONodes 2 "There should be 2 IONodes"
                Expect.equal graph.IONodes.[0].Id "#Test_IO_Node_ID" "First IONode Id should match"
                Expect.equal graph.IONodes.[1].Id data.Id "Second IONode Id should match"
            testCase "GetFirstInputs" <| fun _ ->
                Expect.hasLength graph.FirstNodes 1 "There should be 1 first input"
                Expect.equal graph.FirstNodes.[0].Id sample.Id "First input Id should match"
            testCase "GetFinalOutputs" <| fun _ ->
                Expect.hasLength graph.LastNodes 1 "There should be 1 final output"
                Expect.equal graph.LastNodes.[0].Id data.Id "Final output Id should match"
            testCase "GetFirstData" <| fun _ ->
                Expect.hasLength graph.FirstData 1 "There should be 1 first data node"
                Expect.equal graph.FirstData.[0].Id data.Id "First data node Id should match"
            testCase "GetFinalData" <| fun _ ->
                Expect.hasLength graph.LastData 1 "There should be 1 final data node"
                Expect.equal graph.LastData.[0].Id data.Id "Final data node Id should match"
            testCase "GetFirstSample" <| fun _ ->
                Expect.hasLength graph.FirstSamples 1 "There should be 1 first sample node"
                Expect.equal graph.FirstSamples.[0].Id sample.Id "First sample node Id should match"
            testCase "GetFinalSample" <| fun _ ->
                Expect.hasLength graph.LastSamples 1 "There should be 1 final sample node"
                Expect.equal graph.LastSamples.[0].Id sample.Id "Final sample node Id should match"
        ]
        testList "ParallelProcesses" [
            let sample1 = LDSample.create(name = "Test IO Node 1", id = "#Test_IO_Node_ID_1")
            let sample2 = LDSample.create(name = "Test IO Node 2", id = "#Test_IO_Node_ID_2")
            let data1 = LDFile.create(name = "MyFile1.txt")
            let data2 = LDFile.create(name = "MyFile2.txt")
            let proc1 = LDLabProcess.create(name = "Test Process 1", id = "#Test_Process_ID_1", objects = ResizeArray [sample1], results = ResizeArray[data1])
            let proc2 = LDLabProcess.create(name = "Test Process 2", id = "#Test_Process_ID_2", objects = ResizeArray [sample2], results = ResizeArray[data2])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc1; proc2])
            let graph = QGraph(dataset.Flatten())
            testCase "Get IONodes" <| fun _ ->
                Expect.hasLength graph.IONodes 4 "There should be 4 IONodes"
                Expect.equal graph.IONodes.[0].Id sample1.Id "First IONode Id should match"
                Expect.equal graph.IONodes.[1].Id data1.Id "Second IONode Id should match"
                Expect.equal graph.IONodes.[2].Id sample2.Id "Third IONode Id should match"
                Expect.equal graph.IONodes.[3].Id data2.Id "Fourth IONode Id should match"
            testCase "GetFirstInputs" <| fun _ ->
                Expect.hasLength graph.FirstNodes 2 "There should be 2 first inputs"
                Expect.equal graph.FirstNodes.[0].Id sample1.Id "First input Id should match"
                Expect.equal graph.FirstNodes.[1].Id sample2.Id "Second input Id should match"
            testCase "GetFinalOutputs" <| fun _ ->
                Expect.hasLength graph.LastNodes 2 "There should be 2 final outputs"
                Expect.equal graph.LastNodes.[0].Id data1.Id "First final output Id should match"
                Expect.equal graph.LastNodes.[1].Id data2.Id "Second final output Id should match"
            testCase "GetFirstData" <| fun _ ->
                Expect.hasLength graph.FirstData 2 "There should be 2 first data nodes"
                Expect.equal graph.FirstData.[0].Id data1.Id "First data node Id should match"
                Expect.equal graph.FirstData.[1].Id data2.Id "Second data node Id should match"
            testCase "GetFinalData" <| fun _ ->
                Expect.hasLength graph.LastData 2 "There should be 2 final data nodes"
                Expect.equal graph.LastData.[0].Id data1.Id "First data node Id should match"
                Expect.equal graph.LastData.[1].Id data2.Id "Second data node Id should match"
            testCase "GetFirstSample" <| fun _ ->
                Expect.hasLength graph.FirstSamples 2 "There should be 2 first sample nodes"
                Expect.equal graph.FirstSamples.[0].Id sample1.Id "First sample node Id should match"
                Expect.equal graph.FirstSamples.[1].Id sample2.Id "Second sample node Id should match"
            testCase "GetFinalSample" <| fun _ ->
                Expect.hasLength graph.LastSamples 2 "There should be 2 final sample nodes"
                Expect.equal graph.LastSamples.[0].Id sample1.Id "First sample node Id should match"
                Expect.equal graph.LastSamples.[1].Id sample2.Id "Second sample node Id should match"
        ]
        testList "SequentialProcesses" [
            let sample1 = LDSample.create(name = "Test IO Node 1", id = "#Test_IO_Node_ID_1")
            let sample2 = LDSample.create(name = "Test IO Node 2", id = "#Test_IO_Node_ID_2")

            let proc1 = LDLabProcess.create(name = "Test Process 1", id = "#Test_Process_ID_1", objects = ResizeArray [sample1], results = ResizeArray[sample2])
            let data1 = LDFile.create(name = "MyFile1.txt")
            let proc2 = LDLabProcess.create(name = "Test Process 2", id = "#Test_Process_ID_2", objects = ResizeArray [sample2], results = ResizeArray[data1])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc1; proc2])
            let graph = QGraph(dataset.Flatten())
            testCase "Get IONodes" <| fun _ ->
                Expect.hasLength graph.IONodes 3 "There should be 4 IONodes"
                Expect.equal graph.IONodes.[0].Id sample1.Id "First IONode Id should match"
                Expect.equal graph.IONodes.[1].Id sample2.Id "Second IONode Id should match"
                Expect.equal graph.IONodes.[2].Id data1.Id "Third IONode Id should match"
            testCase "GetFirstInputs" <| fun _ ->
                Expect.hasLength graph.FirstNodes 1 "There should be 1 first input"
                Expect.equal graph.FirstNodes.[0].Id sample1.Id "First input Id should match"
            testCase "GetFinalOutputs" <| fun _ ->
                Expect.hasLength graph.LastNodes 1 "There should be 1 final output"
                Expect.equal graph.LastNodes.[0].Id data1.Id "Final output Id should match"
            testCase "GetFirstData" <| fun _ ->
                Expect.hasLength graph.FirstData 1 "There should be 1 first data node"
                Expect.equal graph.FirstData.[0].Id data1.Id "First data node Id should match"
            testCase "GetFinalData" <| fun _ ->
                Expect.hasLength graph.LastData 1 "There should be 1 final data node"
                Expect.equal graph.LastData.[0].Id data1.Id "Final data node Id should match"
            testCase "GetFirstSample" <| fun _ ->
                Expect.hasLength graph.FirstSamples 1 "There should be 1 first sample node"
                Expect.equal graph.FirstSamples.[0].Id sample1.Id "First sample node Id should match"
            testCase "GetFinalSample" <| fun _ ->
                Expect.hasLength graph.LastSamples 1 "There should be 1 final sample node"
                Expect.equal graph.LastSamples.[0].Id sample2.Id "Final sample node Id should match"
        ]

    ]


let getValues =
    testList "valueRetrieval" [
        testList "SingleProcess" [
            let characteristic = LDPropertyValue.createCharacteristicValue(name = "organism", value = "E. coli")
            let parameter = LDPropertyValue.createParameterValue(name = "temperature", value = "37", unitText = "degree Celsius")
            let factor = LDPropertyValue.createFactorValue(name = "pH", value = "7.0")
            let componentV = LDPropertyValue.createComponent(name = "medium", value = "LB broth")
            
            let sample = LDSample.create(name = "Test IO Node", id = "#Test_IO_Node_ID", additionalProperties = ResizeArray [characteristic])
            let data = LDFile.create(name = "MyFile.txt")
            data.SetProperty(LDSample.additionalProperty, ResizeArray [factor])
            let protocol = LDLabProtocol.create(name = "Test QLab Protocol", id = "#Test_QLab_Protocol_ID", labEquipments = ResizeArray [componentV])
            let proc = LDLabProcess.create(name = "Test Process", id = "#Test_Process_ID", objects = ResizeArray [sample], results = ResizeArray[data], executesLabProtocol = protocol, parameterValues = ResizeArray [parameter])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc])
            let graph = QGraph(dataset.Flatten())

            testCase "Values" <| fun _ ->
                let values = graph.Values()
                Expect.hasLength values 4 "There should be 4 values in total"
                Expect.hasLength (values.Characteristics()) 1 "There should be 1 characteristic"
                Expect.equal (values.Characteristics().[0].NameText) "organism" "Characteristic name should match"
                Expect.hasLength (values.Parameters()) 1 "There should be 1 parameter"
                Expect.equal (values.Parameters().[0].NameText) "temperature" "Parameter name should match"
                Expect.hasLength (values.Factors()) 1 "There should be 1 factor"
                Expect.equal (values.Factors().[0].NameText) "pH" "Factor name should match"
                Expect.hasLength (values.Components()) 1 "There should be 1 component"
                Expect.equal (values.Components().[0].NameText) "medium" "Component name should match"
            testCase "Characteristics" <| fun _ ->
                let characteristics = graph.Characteristics()
                Expect.hasLength characteristics 1 "There should be 1 characteristic"
                Expect.equal (characteristics.[0].NameText) "organism" "Characteristic name should match"
            testCase "Parameters" <| fun _ ->
                let parameters = graph.Parameters()
                Expect.hasLength parameters 1 "There should be 1 parameter"
                Expect.equal (parameters.[0].NameText) "temperature" "Parameter name should match"
            testCase "Factors" <| fun _ ->
                let factors = graph.Factors()
                Expect.hasLength factors 1 "There should be 1 factor"
                Expect.equal (factors.[0].NameText) "pH" "Factor name should match"
            testCase "Components" <| fun _ ->
                let components = graph.Components()
                Expect.hasLength components 1 "There should be 1 component"
                Expect.equal (components.[0].NameText) "medium" "Component name should match"                          
            ]
    ]

let getNodesOf = testList "getNodesOf" [
        testList "ParallelProcesses" [
            /// sample1 -proc1-> sample2 -proc2-> data1 -proc3-> data2
            /// sample3 -proc4-> sample4 -proc5-> data3 -proc6-> data4
            let sample1 = LDSample.create(name = "Test IO Node 1", id = "#Test_IO_Node_ID_1")
            let sample2 = LDSample.create(name = "Test IO Node 2", id = "#Test_IO_Node_ID_2")
            let data1 = LDFile.create(name = "MyFile1.txt")
            let data2 = LDFile.create(name = "MyFile2.txt")
            let proc1 = LDLabProcess.create(name = "Test Process 1", id = "#Test_Process_ID_1", objects = ResizeArray [sample1], results = ResizeArray[sample2])
            let proc2 = LDLabProcess.create(name = "Test Process 2", id = "#Test_Process_ID_2", objects = ResizeArray [sample2], results = ResizeArray[data1])
            let proc3 = LDLabProcess.create(name = "Test Process 3", id = "#Test_Process_ID_3", objects = ResizeArray [data1], results = ResizeArray[data2])

            let sample3 = LDSample.create(name = "Test IO Node 3", id = "#Test_IO_Node_ID_3")
            let sample4 = LDSample.create(name = "Test IO Node 4", id = "#Test_IO_Node_ID_4")
            let data3 = LDFile.create(name = "MyFile3.txt")
            let data4 = LDFile.create(name = "MyFile4.txt")
            let proc4 = LDLabProcess.create(name = "Test Process 4", id = "#Test_Process_ID_4", objects = ResizeArray [sample3], results = ResizeArray[sample4])
            let proc5 = LDLabProcess.create(name = "Test Process 5", id = "#Test_Process_ID_5", objects = ResizeArray [sample4], results = ResizeArray[data3])
            let proc6 = LDLabProcess.create(name = "Test Process 6", id = "#Test_Process_ID_6", objects = ResizeArray [data3], results = ResizeArray[data4])

            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc1; proc2; proc3; proc4; proc5; proc6])
            let graph = QGraph(dataset.Flatten())
            testCase "GetNodesOf sample2" <| fun _ ->
                let ioSample2 = graph.GetIONodeById(sample2.Id)
                let nodesOfSample2 = ioSample2.GetNodes()
                Expect.hasLength nodesOfSample2 3 "There should be 3 nodes associated with sample2"
                Expect.equal nodesOfSample2.[0].Id sample1.Id "First associated node should be sample1"
                Expect.equal nodesOfSample2.[1].Id data1.Id "Second associated node should be data1"
                Expect.equal nodesOfSample2.[2].Id data2.Id "Third associated node should be data2"
            testCase "GetPreviousNodesOf data3" <| fun _ ->
                let ioData3 = graph.GetIONodeById(data3.Id)
                let previousNodesOfData3 = ioData3.GetPreviousNodes()
                Expect.hasLength previousNodesOfData3 2 "There should be 2 previous nodes associated with data3"
                Expect.equal previousNodesOfData3.[0].Id sample3.Id "First previous node should be sample3"
                Expect.equal previousNodesOfData3.[1].Id sample4.Id "Second previous node should be sample4"
            testCase "GetSucceedingNodesOf data1" <| fun _ ->
                let ioData1 = graph.GetIONodeById(data1.Id)
                let succeedingNodesOfData1 = ioData1.GetSucceedingNodes()
                Expect.hasLength succeedingNodesOfData1 1 "There should be 2 succeeding nodes associated with data1"
                Expect.equal succeedingNodesOfData1.[0].Id data2.Id "First succeeding node should be data2"
            testCase "GetFirstNodesOf sample2" <| fun _ ->
                let ioSample2 = graph.GetIONodeById(sample2.Id)
                let firstNodeOfSample2 = ioSample2.GetFirstNodes()
                Expect.hasLength firstNodeOfSample2 1 "There should be 1 first node associated with sample2"
                Expect.equal firstNodeOfSample2.[0].Id sample1.Id "First node associated with sample2 should be sample1"
            testCase "GetLastNodesOf sample3" <| fun _ ->
                let ioSample3 = graph.GetIONodeById(sample3.Id)
                let lastNodeOfSample3 = ioSample3.GetLastNodes()
                Expect.hasLength lastNodeOfSample3 1 "There should be 1 last node associated with sample3"
                Expect.equal lastNodeOfSample3.[0].Id data4.Id "Last node associated with sample3 should be data4"
        ]
        testList "MergingProcesses" [
            /// sample1 -proc1-> sample2 -proc2-> data1 -proc3-> data2
            /// sample3 -proc4-> ""
            let sample1 = LDSample.create(name = "Test IO Node 1", id = "#Test_IO_Node_ID_1")
            let sample2 = LDSample.create(name = "Test IO Node 2", id = "#Test_IO_Node_ID_2")
            let data1 = LDFile.create(name = "MyFile1.txt")
            let data2 = LDFile.create(name = "MyFile2.txt")
            let proc1 = LDLabProcess.create(name = "Test Process 1", id = "#Test_Process_ID_1", objects = ResizeArray [sample1], results = ResizeArray[sample2])
            let proc2 = LDLabProcess.create(name = "Test Process 2", id = "#Test_Process_ID_2", objects = ResizeArray [sample2], results = ResizeArray[data1])
            let proc3 = LDLabProcess.create(name = "Test Process 3", id = "#Test_Process_ID_3", objects = ResizeArray [data1], results = ResizeArray[data2])
            let sample3 = LDSample.create(name = "Test IO Node 3", id = "#Test_IO_Node_ID_3")
            let proc4 = LDLabProcess.create(name = "Test Process 4", id = "#Test_Process_ID_4", objects = ResizeArray [sample3], results = ResizeArray[])
            let dataset = LDDataset.create(name = "Test Dataset", id = "#Test_Dataset_ID", abouts = ResizeArray [proc1; proc2; proc3; proc4])
            let graph = QGraph(dataset.Flatten())
            testCase "GetNodesOf sample2" <| fun _ ->
                let ioSample2 = graph.GetIONodeById(sample2.Id)
                let nodesOfSample2 = ioSample2.GetNodes()
                Expect.hasLength nodesOfSample2 4 "There should be 4 nodes associated with sample2"
                Expect.equal nodesOfSample2.[0].Id sample1.Id "First associated node should be sample1"
                Expect.equal nodesOfSample2.[1].Id sample3.Id "Second associated node should be sample3"
                Expect.equal nodesOfSample2.[2].Id data1.Id "Third associated node should be data1"
                Expect.equal nodesOfSample2.[3].Id data2.Id "Fourth associated node should be data2"
            testCase "GetPreviousNodesOf data1" <| fun _ ->
                let ioData2 = graph.GetIONodeById(data1.Id)
                let previousNodesOfData1 = ioData2.GetPreviousNodes()
                Expect.hasLength previousNodesOfData1 3 "There should be 3 previous nodes associated with data1"
                Expect.equal previousNodesOfData1.[0].Id sample1.Id "First previous node should be sample1"
                Expect.equal previousNodesOfData1.[1].Id sample3.Id "Second previous node should be sample3"
                Expect.equal previousNodesOfData1.[2].Id sample2.Id "Third previous node should be sample2"
            testCase "GetSucceedingNodesOf sample3" <| fun _ ->
                let ioData1 = graph.GetIONodeById(sample3.Id)
                let succeedingNodesOfSample3 = ioData1.GetSucceedingNodes()
                Expect.hasLength succeedingNodesOfSample3 3 "There should be 3 succeeding nodes associated with sample3"
                Expect.equal succeedingNodesOfSample3.[0].Id sample2.Id "First succeeding node should be sample2"
                Expect.equal succeedingNodesOfSample3.[1].Id data1.Id "Second succeeding node should be data1"
                Expect.equal succeedingNodesOfSample3.[2].Id data2.Id "Third succeeding node should be data2"
            testCase "GetFirstNodesOf sample2" <| fun _ ->
                let ioSample2 = graph.GetIONodeById(sample2.Id)
                let firstNodeOfSample2 = ioSample2.GetFirstNodes()
                Expect.hasLength firstNodeOfSample2 2 "There should be 2 first nodes associated with sample2"
                Expect.equal firstNodeOfSample2.[0].Id sample1.Id "First node associated with sample2 should be sample1"
                Expect.equal firstNodeOfSample2.[1].Id sample3.Id "Second node associated with sample2 should be sample3"
            testCase "GetLastNodesOf sample3" <| fun _ ->
                let ioSample3 = graph.GetIONodeById(sample3.Id)
                let lastNodeOfSample3 = ioSample3.GetLastNodes()
                Expect.hasLength lastNodeOfSample3 1 "There should be 1 last node associated with sample3"
                Expect.equal lastNodeOfSample3.[0].Id data2.Id "Last node associated with sample3 should be data2"
        ]
        
    ]


let main = testList "ProcessCore" [
    constructors
    getNodes
    getValues
    getNodesOf
]