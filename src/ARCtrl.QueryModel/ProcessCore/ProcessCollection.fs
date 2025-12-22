namespace rec ARCtrl.QueryModel.ProcessCore

open ARCtrl
open Fable.Core
open ARCtrl.ROCrate
open ARCtrl.QueryModel
open System.Collections.Generic

[<AttachMembers>]
type ProcessSequence(processes : ResizeArray<QLabProcess>) =
    
    let mutable _processMap = 
        Dictionary<string, QLabProcess>()
        |> fun d -> 
            processes 
            |> ResizeArray.iter (fun p -> d.Add(p.Id,p))
            d

    member this.Processes = processes

    member this.HasProcess(id : string) =
        _processMap.ContainsKey(id)

    member this.TryGetProcess(id : string) =
        match _processMap.TryGetValue(id) with
        | (true,p) -> Some p
        | _ -> None

    member this.GetProcess(id : string) =
        _processMap.[id]

    //static member getProcesses(?ps : ProcessSequence) = 
    //    match ps with
    //    | Some ps -> ps
    //    | None -> 
    //        graph.Nodes
    //        |> ResizeArray.choose (fun n -> 
    //            if LDLabProcess.validate(n, context = context) then
    //                Some (QLabProcess(n))
    //            else
    //                None
    //        )
    //        |> ProcessSequence


/// Type representing a queryable collection of processes, which model the experimental graph
[<AttachMembers>]
type QGraph(inGraph : LDGraph) as this =
       
    inherit LDGraph(?id = inGraph.Id, nodes = inGraph.Nodes, ?context = inGraph.TryGetContext())

    let mutable context = 
        match this.TryGetContext() with
        | Some c -> c
        | None ->
            LDContext(
                baseContexts = ResizeArray[
                    Context.initBioschemasContext()
                    Context.initV1_2()
                ]
            )

    do 
        this.Nodes
        |> Seq.iter (fun n ->
            if LDLabProcess.validate(n, context = context) then
                let inputs = LDLabProcess.getObjects(n, graph = this, context = context)
                let outputs = LDLabProcess.getResults(n, graph = this, context = context)
                inputs
                |> Seq.iter (fun inputNode ->
                    let objectOfs = inputNode.GetPropertyValues(objectOf, context = context)
                    objectOfs.Add(LDRef(n.Id))
                    objectOfs
                    //|> ResizeArray.distinct
                    |> Seq.distinct
                    |> ResizeArray
                    |> fun v -> inputNode.SetProperty(objectOf, v, context = context)  
                )
                outputs
                |> Seq.iter (fun outputNode ->
                    let resultOfs = outputNode.GetPropertyValues(resultOf, context = context)
                    resultOfs.Add(LDRef(n.Id))
                    resultOfs
                    //|> ResizeArray.distinct
                    |> Seq.distinct
                    |> ResizeArray
                    |> fun v -> outputNode.SetProperty(resultOf, v, context = context)
                )   
        )

    member this.ProcessSequence = 
        this.Nodes
        |> ResizeArray.choose (fun n -> 
            if LDLabProcess.validate(n, context = context) then
                Some (QLabProcess(n, parentGraph = this))
            else
                None
        )
        |> ProcessSequence

    member this.Context
        with get() = context
        and set(v) = context <- v

    static member nodeIsRoot (node : IONode, ?ps : ProcessSequence) =
        match ps with
        | Some ps ->           
            node.ResultOf()
            |> Seq.exists (fun (p : LDNode) -> ps.HasProcess p.Id)
            |> not
        | None ->
            node.ResultOf()
            |> Seq.isEmpty



    static member nodeIsFinal (node : IONode, ?ps : ProcessSequence) =
        match ps with
        | Some ps ->
            node.ObjectOf()
            |> Seq.exists (fun (p : LDNode) -> ps.HasProcess p.Id)
            |> not
        | None ->
            node.ObjectOf()
            |> Seq.isEmpty


    /// Returns the list of all nodes (sources, samples, data) in the ProcessSequence
    static member getNodes (graph : QGraph, ?ps : ProcessSequence) =
        match ps with
        | Some ps -> 
            ps.Processes        
            |> ResizeArray.collect (fun p -> 
                ResizeArray.append p.Inputs p.Outputs
            )
            //|> ResizeArray.distinct     
            |> Seq.distinct
            |> ResizeArray
        | None ->
            graph.Nodes
            |> ResizeArray.choose (fun n -> 
                if LDSample.validate(n, context = graph.Context) then
                    Some (IONode(n,parentGraph = graph))
                elif LDFile.validate(n, context = graph.Context) then
                    Some (IONode(n,parentGraph = graph))
                else
                    None
            )



    static member collectBackwards (node : IONode, f:QLabProcess -> ResizeArray<'A>, graph : QGraph, ?ps : ProcessSequence) : ResizeArray<'A> = 
        let ps = ps |> Option.defaultValue (graph.ProcessSequence)
        let resultNodes = HashSet<IONode>()
        let result = HashSet<'A>()
        let rec loop (toCheck : ResizeArray<IONode>) =
            if toCheck.Count = 0 then
                ResizeArray(result)
            else
                toCheck
                |> ResizeArray.collect (fun n ->
                    if resultNodes.Contains n then
                        ResizeArray []
                    else
                        resultNodes.Add n |> ignore
                        n.ResultOf()
                        |> ResizeArray.collect (fun (pn : LDNode) ->
                            if ps.HasProcess(pn.Id) then
                                let p = ps.GetProcess(pn.Id)
                                f p
                                |> ResizeArray.iter (fun r -> result.Add r |> ignore)
                                p.Inputs
                            else 
                                ResizeArray []
                        )
                )
                |> loop
        loop (ResizeArray [node])


    static member collectForwards (node : IONode, f:QLabProcess -> ResizeArray<'A>, graph : QGraph, ?ps : ProcessSequence) : ResizeArray<'A> = 
        let ps = ps |> Option.defaultValue (graph.ProcessSequence)
        let resultNodes = HashSet<IONode>()
        let result = HashSet<'A>()
        let rec loop (toCheck : ResizeArray<IONode>) =
            if toCheck.Count = 0 then
                ResizeArray(result)
            else
                toCheck
                |> ResizeArray.collect (fun n ->
                    if resultNodes.Contains n then
                        ResizeArray []
                    else
                        resultNodes.Add n |> ignore
                        n.ObjectOf()
                        |> ResizeArray.collect (fun (pn : LDNode) ->
                            if ps.HasProcess(pn.Id) then
                                let p = ps.GetProcess(pn.Id)
                                f p
                                |> ResizeArray.iter (fun r -> result.Add r |> ignore)
                                p.Outputs
                            else 
                                ResizeArray []
                        )
                )
                |> loop
        loop (ResizeArray [node])

    static member getPreviousProcessesOf (node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.collectBackwards(node, id >> ResizeArray.singleton, graph = graph, ?ps = ps)
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    static member getSucceedingProcessesOf (node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.collectForwards(node, id >> ResizeArray.singleton, graph = graph, ?ps = ps)
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    /// Returns a new process sequence, only with those rows that contain either an educt or a product entity of the given node (or entity)
    static member getProcessesOf (node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        let forwardProcesses = 
            QGraph.collectForwards(node, id >> ResizeArray.singleton, graph = graph, ?ps = ps)
        let backwardProcesses = 
            QGraph.collectBackwards(node, id >> ResizeArray.singleton, graph = graph, ?ps = ps)
        ResizeArray.append forwardProcesses backwardProcesses
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints
    static member getRootInputs (graph : QGraph, ?ps : ProcessSequence) =
        let f n = QGraph.nodeIsRoot(n, ?ps = ps)
        QGraph.getNodesBy(f, graph = graph, ?ps = ps)

    /// Returns the names of all final outputs of the processSequence, which point to no further nodes
    static member getFinalOutputs (graph : QGraph, ?ps : ProcessSequence) =
        let f n = QGraph.nodeIsFinal(n, ?ps = ps)
        QGraph.getNodesBy(f, graph = graph, ?ps = ps)

    /// Returns the names of all nodes for which the predicate returns true
    static member getNodesBy (predicate : IONode -> bool, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.getNodes(graph = graph, ?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)


    /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints, and for which the predicate returns true
    static member getRootInputsBy (predicate : IONode -> bool, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.getRootInputs(graph = graph, ?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)

    /// Returns the names of all final outputs of the processSequence, which point to no further nodes, and for which the predicate returns true
    static member getFinalOutputsBy (predicate : IONode -> bool, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.getFinalOutputs(graph = graph, ?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)

    static member getPreviousNodesOf (node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.collectBackwards (node, QLabProcess.inputs, graph = graph, ?ps = ps)

    static member getSucceedingNodesOf (node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.collectForwards (node, QLabProcess.outputs, graph = graph, ?ps = ps)

    /// Returns the names of all nodes processSequence, which are connected to the given node and for which the predicate returns true
    static member getNodesOfBy (predicate : IONode -> bool, node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        QGraph.getSucceedingNodesOf(node, graph = graph, ?ps = ps)
        |> ResizeArray.append (QGraph.getPreviousNodesOf(node, graph = graph, ?ps = ps))
        |> ResizeArray.filter (fun n -> predicate n)

    /// Returns the initial inputs final outputs of the assay, to which no processPoints, which are connected to the given node and for which the predicate returns true
    static member getRootInputsOfBy (predicate : IONode -> bool, node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            QLabProcess.inputs p           
            |> ResizeArray.filter (fun input ->
                QGraph.nodeIsRoot(input, ?ps = ps) && predicate input
            )
        QGraph.collectBackwards(node, f, graph = graph, ?ps = ps)

    /// Returns the final outputs of the assay, which point to no further nodes, which are connected to the given node and for which the predicate returns true
    static member getFinalOutputsOfBy (predicate : IONode -> bool, node : IONode, graph : QGraph, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            QLabProcess.outputs p
            |> ResizeArray.filter (fun output ->
                QGraph.nodeIsFinal(output, ?ps = ps) && predicate output
            )
        QGraph.collectForwards(node, f, graph = graph, ?ps = ps)
       
    static member getValues (graph : QGraph, ?ps : ProcessSequence) =
        let ps = ps |> Option.defaultValue (graph.ProcessSequence)
        ps.Processes
        |> ResizeArray.collect (fun p -> p.Values)
        |> QValueCollection

    /// Returns the previous values of the given node
    static member getPreviousValuesOf (node : IONode, graph : QGraph, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                p.Values
        QGraph.collectBackwards(node, f, graph = graph, ?ps = ps)
        |> QValueCollection

    /// Returns the succeeding values of the given node
    static member getSucceedingValuesOf (node : IONode, graph : QGraph, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                p.Values
        QGraph.collectForwards(node, f, graph = graph, ?ps = ps)
        |> QValueCollection

    static member getValuesOf (node : IONode, graph : QGraph, ?protocolName : string, ?ps : ProcessSequence) =
        (QGraph.getSucceedingValuesOf(node, graph = graph, ?protocolName = protocolName, ?ps = ps).Values)
        |> ResizeArray.append (QGraph.getPreviousValuesOf(node, graph = graph, ?protocolName = protocolName, ?ps = ps).Values) 
        |> QValueCollection

    /// Returns a new ProcessSequence, with only the values from the processes that implement the given protocol
    static member onlyValuesOfProtocol (protocolName : string, graph : QGraph, ?ps : ProcessSequence) : QValueCollection =
        ps 
        |> Option.defaultValue (graph.ProcessSequence)
        |> fun ps -> ps.Processes
        |> ResizeArray.collect (fun p -> 
            match p.Protocol with
            | Some pt when pt.Name = protocolName ->
                p.Values
            | _ -> ResizeArray []
        )
        |> QValueCollection

    /// Returns the names of all nodes in the Process sequence
    member this.NodesOf(node : IONode) =
        QGraph.getNodesOfBy((fun _ -> true),node,this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
    member this.FirstNodesOf(node : IONode) = 
        QGraph.getRootInputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
    member this.LastNodesOf(node : IONode) = 
        QGraph.getFinalOutputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all samples in the Process sequence, that are connected to the given node
    member this.SamplesOf(node : IONode) =
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
    member this.FirstSamplesOf(node : IONode) = 
        QGraph.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
    member this.LastSamplesOf(node : IONode) = 
        QGraph.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all sources in the Process sequence, that are connected to the given node
    member this.SourcesOf(node : IONode) =
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsSource), node, this)

    /// Returns the names of all data in the Process sequence, that are connected to the given node
    member this.DataOf(node : IONode) =
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
    member this.FirstDataOf(node : IONode) = 
        QGraph.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
    member this.LastDataOf(node: IONode) = 
        QGraph.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns all values in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Values(?protocolName) = 
        match protocolName with
        | Some pn ->
            QGraph.onlyValuesOfProtocol(pn, this)
        | None ->
            QGraph.getValues(this)


    /// Returns all values in the process sequence whose header matches the given category
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Values(ontology : OntologyAnnotation, ?protocolName) = 
        this.Values(?protocolName = protocolName).WithCategory(ontology)

    /// Returns all values in the process sequence whose header matches the given name
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Values(name : string, ?protocolName) = 
        this.Values(?protocolName = protocolName).WithName(name)

    /// Returns all factor values in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Factors(?protocolName) =
        this.Values(?protocolName = protocolName).Factors()

    /// Returns all parameter values in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Parameters(?protocolName) =
        this.Values(?protocolName = protocolName).Parameters()

    /// Returns all characteristic values in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Characteristics(?protocolName) =
        this.Values(?protocolName = protocolName).Characteristics()

    /// Returns all components in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Components(?protocolName) =
        this.Values(?protocolName = protocolName).Components()

    
    /// Returns all values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousValuesOf(node : IONode, ?protocolName) =
        QGraph.getPreviousValuesOf(node, graph = this, ?protocolName = protocolName, ps = this.ProcessSequence)

    /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingValuesOf(node : IONode, ?protocolName) =
        QGraph.getSucceedingValuesOf(node, graph = this, ?protocolName = protocolName, ps = this.ProcessSequence)

    /// Returns all values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ValuesOf(node : IONode, ?protocolName : string) =
        ResizeArray.append (this.PreviousValuesOf(node,?protocolName = protocolName).Values) (this.SucceedingValuesOf(node,?protocolName = protocolName).Values)
        |> QValueCollection

    /// Returns all characteristic values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.CharacteristicsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousCharacteristicsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingCharacteristicsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all parameter values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ParametersOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousParametersOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingParametersOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.FactorsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousFactorsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol and come after it in the sequence
    member this.SucceedingFactorsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all components values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ComponentsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousComponentsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingComponentsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Components()

    member this.ContainsByCategory(category : OntologyAnnotation, ?protocolName) = 
            this.Values(?protocolName = protocolName).ContainsByCategory category

    member this.ContainsByName(name : string, ?protocolName) = 
            this.Values(?protocolName = protocolName).ContainsByName name

    /// Returns the names of all nodes in the Process sequence
    member this.IONodes =
        QGraph.getNodes(this)

    member this.c(name : string) =
        this.IONodes
        |> Seq.find (fun n -> n.Name = name)

    member this.GetIONodeById(id : string) =
        this.GetNode(id)
        |> fun n -> IONode(n, parentGraph = this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points
    member this.FirstNodes = 
        QGraph.getRootInputs(this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input
    member this.LastNodes = 
        QGraph.getFinalOutputs(this)

    /// Returns the names of all samples in the Process sequence
    member this.Samples =
        QGraph.getNodesBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all the input samples in the Process sequence to which no output points
    member this.FirstSamples = 
        QGraph.getRootInputsBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all the output samples in the Process sequence that point to no input
    member this.LastSamples = 
        QGraph.getFinalOutputsBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all sources in the Process sequence
    member this.Sources =
        QGraph.getNodesBy ((fun (io : IONode) -> io.IsSource),this)

    /// Returns the names of all data in the Process sequence
    member this.Data =
        QGraph.getNodesBy ((fun (io : IONode) -> io.IsFile),this)

    /// Returns the names of all the input data in the Process sequence to which no output points
    member this.FirstData = 
        QGraph.getRootInputsBy ((fun (io : IONode) -> io.IsFile),this)

    /// Returns the names of all the output data in the Process sequence that point to no input
    member this.LastData = 
        QGraph.getFinalOutputsBy ((fun (io : IONode) -> io.IsFile),this)

[<AttachMembers>]
type QLabProcess(node : LDNode, ?parentGraph : QGraph) as this = 


    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    let context() = parentGraph |> Option.map (fun g -> g.Context)

    do 
        
        if LDLabProcess.validate(node, ?context = context()) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.GetProperties(false)
        |> Seq.iter (fun kv ->
            if not (kv.Key = "Id") then
                this.SetProperty(kv.Key, kv.Value)
        )
        //node.DeepCopyPropertiesTo(this)

    static member name (labProcess : QLabProcess) = labProcess.Name

    static member inputs (labProcess : QLabProcess) = labProcess.Inputs

    static member outputs (labProcess : QLabProcess) = labProcess.Outputs

    static member inputNames (labProcess : QLabProcess) = labProcess.InputNames

    static member outputNames (labProcess : QLabProcess) = labProcess.OutputNames

    static member inputTypes (labProcess : QLabProcess)  = labProcess.InputTypes

    static member outputTypes (labProcess : QLabProcess) = labProcess.OutputTypes

    member this.ParentGraph = parentGraph

    member this.Name = 
        LDLabProcess.getNameAsString(this, ?context = context())

    member this.Inputs = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDLabProcess.getObjects(this, ?graph = parentGraph, ?context = context())
        |> ResizeArray.map (fun ioNode -> IONode(ioNode, ?parentGraph = this.ParentGraph))

    member this.Outputs = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDLabProcess.getResults(this, ?graph = parentGraph, ?context = context())
        |> ResizeArray.map (fun ioNode -> IONode(ioNode, ?parentGraph = this.ParentGraph))

    member this.InputNames = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        let inputs = LDLabProcess.getObjects(this, ?graph = parentGraph, ?context = context())
        inputs |> ResizeArray.map (fun i -> LDSample.getNameAsString(i, ?context = context()))

    member this.OutputNames = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        let outputs = LDLabProcess.getResults(this, ?graph = parentGraph, ?context = context())
        outputs |> ResizeArray.map (fun i -> LDSample.getNameAsString(i, ?context = context()))

    member this.InputTypes = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        let inputs = LDLabProcess.getObjects(this, ?graph = parentGraph, ?context = context())
        inputs |> ResizeArray.map (fun i -> i.SchemaType)

    member this.OutputTypes = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        let outputs = LDLabProcess.getResults(this, ?graph = parentGraph, ?context = context())
        outputs |> ResizeArray.map (fun i -> i.SchemaType)

    member this.ParameterValues =
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDLabProcess.getParameterValues(this, ?graph = parentGraph, ?context = context())
        |> ResizeArray.map (fun pvNode -> QPropertyValue(pvNode))

    member this.Protocol : QLabProtocol option = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDLabProcess.tryGetExecutesLabProtocol(this, ?graph = parentGraph, ?context = context())
        |> Option.map (fun (lpNode : LDNode) -> QLabProtocol(lpNode))

    member this.Components = 
        match this.Protocol with
        | Some protocolNode ->
            protocolNode.Components
        | None -> ResizeArray()

    member this.GetNextProcesses() =
        this.Inputs
        |> ResizeArray.collect (fun ioNode -> ioNode.ObjectOf())
        //|> ResizeArray.distinctBy (fun (p : LDNode) -> p.Id)
        |> Seq.distinctBy (fun (p : LDNode) -> p.Id)
        |> ResizeArray


    member this.GetPreviousProcesses() =
        this.Outputs
        |> ResizeArray.collect (fun ioNode -> ioNode.ResultOf())
        //|> ResizeArray.distinctBy (fun (p : LDNode) -> p.Id)
        |> Seq.distinctBy (fun (p : LDNode) -> p.Id)
        |> ResizeArray

    member this.Values = 
        (this.Inputs |> ResizeArray.collect (fun i -> i.Characteristics))
        |> ResizeArray.append (this.Outputs |> ResizeArray.collect (fun o -> o.Factors))
        |> ResizeArray.append this.ParameterValues
        |> ResizeArray.append this.Components

    override this.GetHashCode (): int = 
        this.Id.GetHashCode()

    override this.Equals(obj: obj): bool =
        match obj with
        | :? QLabProcess as p -> this.Id = p.Id
        | _ -> false


[<AttachMembers>]
type QLabProtocol(node : LDNode, ?parentGraph : QGraph) as this = 

    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    let context() = parentGraph |> Option.map (fun g -> g.Context)

    do 
        if LDLabProtocol.validate(node, ?context = context()) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.GetProperties(false)
        |> Seq.iter (fun kv ->
            if not (kv.Key = "Id") then
                this.SetProperty(kv.Key, kv.Value)
        )
        //node.DeepCopyPropertiesTo(this)

    static member name (labProtocol : QLabProtocol) =
        labProtocol.Name

    static member components (labProtocol : QLabProtocol) =
        labProtocol.Components

    member this.Name = 
        LDLabProtocol.getNameAsString(this, ?context = context())

    member this.Components = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDLabProtocol.getComponents(this, ?graph = parentGraph, ?context = context())
        |> ResizeArray.map (fun cvNode -> QPropertyValue(cvNode))

    override this.GetHashCode () : int = 
        this.Id.GetHashCode()

    override this.Equals(obj: obj) : bool =
        match obj with
        | :? QLabProtocol as other -> this.Id = other.Id
        | _ -> false

[<AttachMembers>]
type IONode(node : LDNode, ?parentGraph : QGraph) as this =

    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    let context() = parentGraph |> Option.map (fun g -> g.Context)

    do        
        node.GetProperties(false)
        |> Seq.iter (fun kv ->
            if not (kv.Key = "Id") then
                this.SetProperty(kv.Key, kv.Value)
        )
        //node.DeepCopyPropertiesTo(this)

    member this.Name : string = LDSample.getNameAsString(this, ?context = context())

    member this.IsSample = LDSample.validateSample(this, ?context = context())

    member this.IsSource = LDSample.validateSource(this, ?context = context())

    member this.IsMaterial = LDSample.validateMaterial(this, ?context = context())

    member this.IsFile = LDFile.validate(this, ?context = context())

    member this.ParentGraph = parentGraph

    member this.AdditionalProperties = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        LDSample.getAdditionalProperties(this, ?context = context(), ?graph = parentGraph)
        |> ResizeArray.map (fun apNode -> QPropertyValue(apNode))

    member this.Characteristics = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsCharacteristic)

    member this.Factors = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsFactor)

    member this.HasResultOf() = 
       this.HasProperty(resultOf, ?context = context())

    member this.HasObjectOf() =
        this.HasProperty(objectOf, ?context = context())

    member this.ResultOf() = 
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        this.GetPropertyNodes(resultOf, ?graph = parentGraph, ?context = context())

    member this.ObjectOf() =
        #if !FABLE_COMPILER
        let parentGraph = parentGraph |> Option.map (fun g -> g :> LDGraph)
        #endif
        this.GetPropertyNodes(objectOf, ?graph = parentGraph, ?context = context())
        
    /// Returns all other nodes in the process sequence, that are connected to this node
    member this.GetNodes() = 
        if parentGraph.IsNone then failwithf "IONode.GetNodes requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getNodesOfBy((fun _ -> true),this, graph = parentGraph.Value)

    member this.GetPreviousNodes() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousNodes requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousNodesOf(this, graph = parentGraph.Value)

    /// Returns all other nodes in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstNodes() = 
        if parentGraph.IsNone then failwithf "IONode.GetFirstNodes requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getRootInputsOfBy((fun _ -> true),this, graph = parentGraph.Value)

    member this.GetSucceedingNodes() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingNodes requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingNodesOf(this, graph = parentGraph.Value)

    /// Returns all other nodes in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastNodes() = 
        if parentGraph.IsNone then failwithf "IONode.GetLastNodes requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getFinalOutputsOfBy((fun _ -> true),this, graph = parentGraph.Value)

    /// Returns all other samples in the process sequence, that are connected to this node
    member this.GetSamples() = 
        if parentGraph.IsNone then failwithf "IONode.GetSamples requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsSample), this, graph = parentGraph.Value)

    /// Returns all other samples in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstSamples() = 
        if parentGraph.IsNone then failwithf "IONode.GetFirstSamples requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), this, graph = parentGraph.Value)
        
    /// Returns all other samples in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastSamples() = 
        if parentGraph.IsNone then failwithf "IONode.GetLastSamples requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), this, graph = parentGraph.Value)

    /// Returns all other sources in the process sequence, that are connected to this node
    member this.GetSources() = 
        if parentGraph.IsNone then failwithf "IONode.GetSources requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsSource), this, graph = parentGraph.Value)

    /// Returns all other data in the process sequence, that are connected to this node
    member this.GetData() = 
        if parentGraph.IsNone then failwithf "IONode.GetData requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getNodesOfBy ((fun (io : IONode) -> io.IsFile), this, graph = parentGraph.Value)

    /// Returns all other data in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstData() = 
        if parentGraph.IsNone then failwithf "IONode.GetFirstData requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), this, graph = parentGraph.Value)

    /// Returns all other data in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastData() = 
        if parentGraph.IsNone then failwithf "IONode.GetLastData requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsFile), this, graph = parentGraph.Value)

    /// Returns all values in the process sequence, that are connected to this given node
    member this.GetValues() = 
        if parentGraph.IsNone then failwithf "IONode.GetValues requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getValuesOf(this, graph = parentGraph.Value)

    /// Returns all values in the process sequence, that are connected to this given node and come before it in the sequence
    member this.GetPreviousValues() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousValues requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousValuesOf(this, graph = parentGraph.Value)

    /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingValues() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingValues requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingValuesOf(this, graph = parentGraph.Value)

    /// Returns all characteristic values in the process sequence, that are connected to the given node
    member this.GetCharacteristics() = 
        if parentGraph.IsNone then failwithf "IONode.GetCharacteristics requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getValuesOf(this, graph = parentGraph.Value).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousCharacteristics() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousCharacteristics requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousValuesOf(this, graph = parentGraph.Value).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingCharacteristics() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingCharacteristics requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingValuesOf(this, graph = parentGraph.Value).Characteristics()

    /// Returns all parameter values in the process sequence, that are connected to the given node
    member this.GetParameters() = 
        if parentGraph.IsNone then failwithf "IONode.GetParameters requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getValuesOf(this, graph = parentGraph.Value).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousParameters() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousParameters requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousValuesOf(this, graph = parentGraph.Value).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingParameters() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingParameters requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingValuesOf(this, graph = parentGraph.Value).Parameters()

    /// Returns all factor values in the process sequence, that are connected to the given node
    member this.GetFactors() = 
        if parentGraph.IsNone then failwithf "IONode.GetFactors requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getValuesOf(this, graph = parentGraph.Value).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousFactors() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousFactors requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousValuesOf(this, graph = parentGraph.Value).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingFactors() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingFactors requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingValuesOf(this, graph = parentGraph.Value).Factors()

    /// Returns all component values in the process sequence, that are connected to the given node
    member this.GetComponents() = 
        if parentGraph.IsNone then failwithf "IONode.GetComponents requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getValuesOf(this, graph = parentGraph.Value).Components()

    /// Returns all component values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousComponents() = 
        if parentGraph.IsNone then failwithf "IONode.GetPreviousComponents requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousValuesOf(this, graph = parentGraph.Value).Components()

    /// Returns all component values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingComponents() = 
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingComponents requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingValuesOf(this, graph = parentGraph.Value).Components()

    member this.GetProcesses() =
        if parentGraph.IsNone then failwithf "IONode.GetProcesses requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getProcessesOf(this, graph = parentGraph.Value)

    member this.GetPreviousProcesses() =
        if parentGraph.IsNone then failwithf "IONode.GetPreviousProcesses requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getPreviousProcessesOf(this, graph = parentGraph.Value)

    member this.GetSucceedingProcesses() =
        if parentGraph.IsNone then failwithf "IONode.GetSucceedingProcesses requires a parent QGraph to be set. Not set for node \"%s\"" this.Id
        QGraph.getSucceedingProcessesOf(this, graph = parentGraph.Value)

    override this.GetHashCode() =
        this.Id.GetHashCode()

    override this.Equals(obj) =
        match obj with
        | :? IONode as other -> this.Id = other.Id
        | _ -> false