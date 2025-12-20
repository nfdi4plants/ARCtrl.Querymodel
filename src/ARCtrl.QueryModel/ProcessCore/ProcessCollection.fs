namespace rec ARCtrl.QueryModel.ProcessCore

open ARCtrl
open Fable.Core
open ARCtrl.ROCrate
open ARCtrl.QueryModel
open System.Collections.Generic


/// Type representing a queryable collection of processes, which model the experimental graph
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

    static member getProcesses(?ps : ProcessSequence) = 
        match ps with
        | Some ps -> ps
        | None -> 
            graph.Nodes
            |> ResizeArray.choose (fun n -> 
                if LDLabProcess.validate(n, context = context) then
                    Some (QLabProcess(n))
                else
                    None
            )
            |> ProcessSequence

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
    static member getNodes (?ps : ProcessSequence) =
        match ps with
        | Some ps -> 
            ProcessSequence.getProcesses(ps = ps).Processes        
            |> ResizeArray.collect (fun p -> 
                ResizeArray.append p.Inputs p.Outputs
            )
            //|> ResizeArray.distinct     
            |> Seq.distinct
            |> ResizeArray
        | None ->
            graph.Nodes
            |> ResizeArray.choose (fun n -> 
                if LDSample.validate(n, context = context) then
                    Some (IONode(n))
                elif LDFile.validate(n, context = context) then
                    Some (IONode(n))
                else
                    None
            )



    static member collectBackwards (node : IONode, f:QLabProcess -> ResizeArray<'A>,?ps : ProcessSequence) : ResizeArray<'A> = 
        let ps = ProcessSequence.getProcesses(?ps = ps)
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


    static member collectForwards (node : IONode, f:QLabProcess -> ResizeArray<'A>,?ps : ProcessSequence) : ResizeArray<'A> = 
        let ps = ProcessSequence.getProcesses(?ps = ps)
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

    static member getPreviousProcessesOf (node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.collectBackwards(IONode(graph.GetNode(node.Id)), id >> ResizeArray.singleton, ?ps = ps)
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    static member getSucceedingProcessesOf (node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.collectForwards(IONode(graph.GetNode(node.Id)), id >> ResizeArray.singleton, ?ps = ps)
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    /// Returns a new process sequence, only with those rows that contain either an educt or a product entity of the given node (or entity)
    static member getProcessesOf (node : IONode, ?ps : ProcessSequence) =
        let forwardProcesses = 
            ProcessSequence.collectForwards(IONode(graph.GetNode(node.Id)), id >> ResizeArray.singleton, ?ps = ps)
        let backwardProcesses = 
            ProcessSequence.collectBackwards(IONode(graph.GetNode(node.Id)), id >> ResizeArray.singleton, ?ps = ps)
        ResizeArray.append forwardProcesses backwardProcesses
        //|> ResizeArray.distinct
        |> Seq.distinct
        |> ResizeArray
        |> ProcessSequence

    /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints
    static member getRootInputs (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        let f n = ProcessSequence.nodeIsRoot(n, ps = ps)
        ProcessSequence.getNodesBy(f, ps = ps)

    /// Returns the names of all final outputs of the processSequence, which point to no further nodes
    static member getFinalOutputs (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        let f n = ProcessSequence.nodeIsFinal(n, ps = ps)
        ProcessSequence.getNodesBy(f, ps = ps)

    /// Returns the names of all nodes for which the predicate returns true
    static member getNodesBy (predicate : IONode -> bool, ?ps : ProcessSequence) =
        ProcessSequence.getNodes(?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)


    /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints, and for which the predicate returns true
    static member getRootInputsBy (predicate : IONode -> bool,?ps : ProcessSequence) =
        ProcessSequence.getRootInputs(?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)

    /// Returns the names of all final outputs of the processSequence, which point to no further nodes, and for which the predicate returns true
    static member getFinalOutputsBy (predicate : IONode -> bool,?ps : ProcessSequence) =
        ProcessSequence.getFinalOutputs(?ps = ps)
        |> ResizeArray.filter (fun n -> predicate n)

    static member getPreviousNodesOf (node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.collectBackwards (node, QLabProcess.inputs, ?ps = ps)

    static member getSucceedingNodesOf (node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.collectForwards (node, QLabProcess.outputs, ?ps = ps)

    /// Returns the names of all nodes processSequence, which are connected to the given node and for which the predicate returns true
    static member getNodesOfBy (predicate : IONode -> bool, node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.getSucceedingNodesOf(node, ?ps = ps)
        |> ResizeArray.append (ProcessSequence.getPreviousNodesOf(node, ?ps = ps))
        |> ResizeArray.filter (fun n -> predicate n)

    /// Returns the initial inputs final outputs of the assay, to which no processPoints, which are connected to the given node and for which the predicate returns true
    static member getRootInputsOfBy (predicate : IONode -> bool, node : IONode, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            QLabProcess.inputs p           
            |> ResizeArray.filter (fun input ->
                ProcessSequence.nodeIsRoot(input, ?ps = ps) && predicate input
            )
        ProcessSequence.collectBackwards(IONode(graph.GetNode(node.Id)), f, ?ps = ps)

    /// Returns the final outputs of the assay, which point to no further nodes, which are connected to the given node and for which the predicate returns true
    static member getFinalOutputsOfBy (predicate : IONode -> bool, node : IONode, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            QLabProcess.outputs p
            |> ResizeArray.filter (fun output ->
                ProcessSequence.nodeIsFinal(output, ?ps = ps) && predicate output
            )
        ProcessSequence.collectForwards(IONode(graph.GetNode(node.Id)), f, ?ps = ps)
       
    static member getValues (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        ps.Processes
        |> ResizeArray.collect (fun p -> p.Values)
        |> QValueCollection

    /// Returns the previous values of the given node
    static member getPreviousValuesOf (node : IONode, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                p.Values
        ProcessSequence.collectBackwards(IONode(graph.GetNode(node.Id)), f, ?ps = ps)
        |> QValueCollection

    /// Returns the succeeding values of the given node
    static member getSucceedingValuesOf (node : IONode, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                p.Values
        ProcessSequence.collectForwards(IONode(graph.GetNode(node.Id)), f, ?ps = ps)
        |> QValueCollection

    static member getValuesOf (node : IONode, ?protocolName : string, ?ps : ProcessSequence) =
        (ProcessSequence.getSucceedingValuesOf(node, ?protocolName = protocolName, ?ps = ps).Values)
        |> ResizeArray.append (ProcessSequence.getPreviousValuesOf(node, ?protocolName = protocolName, ?ps = ps).Values) 
        |> QValueCollection

    /// Returns a new ProcessSequence, with only the values from the processes that implement the given protocol
    static member onlyValuesOfProtocol (protocolName : string, ?ps : ProcessSequence) : QValueCollection =
        ProcessSequence.getProcesses(?ps = ps).Processes
        |> ResizeArray.collect (fun p -> 
            match p.Protocol with
            | Some pt when pt.Name = protocolName ->
                p.Values
            | _ -> ResizeArray []
        )
        |> QValueCollection

    /// Returns the names of all nodes in the Process sequence
    member this.NodesOf(node : IONode) =
        ProcessSequence.getNodesOfBy((fun _ -> true),node,this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
    member this.FirstNodesOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
    member this.LastNodesOf(node : IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all samples in the Process sequence, that are connected to the given node
    member this.SamplesOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
    member this.FirstSamplesOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
    member this.LastSamplesOf(node : IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all sources in the Process sequence, that are connected to the given node
    member this.SourcesOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSource), node, this)

    /// Returns the names of all data in the Process sequence, that are connected to the given node
    member this.DataOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
    member this.FirstDataOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
    member this.LastDataOf(node: IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns all values in the process sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.Values(?protocolName) = 
        match protocolName with
        | Some pn ->
            ProcessSequence.onlyValuesOfProtocol(pn, this)
        | None ->
            ProcessSequence.getValues(this)


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
        ProcessSequence.getPreviousValuesOf(node, ?protocolName = protocolName, ps = this)

    /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingValuesOf(node : IONode, ?protocolName) =
        ProcessSequence.getSucceedingValuesOf(node, ?protocolName = protocolName, ps = this)

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
    member this.Nodes =
        ProcessSequence.getNodes(this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points
    member this.FirstNodes = 
        ProcessSequence.getRootInputs(this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input
    member this.LastNodes = 
        ProcessSequence.getFinalOutputs(this)

    /// Returns the names of all samples in the Process sequence
    member this.Samples =
        ProcessSequence.getNodesBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all the input samples in the Process sequence to which no output points
    member this.FirstSamples = 
        ProcessSequence.getRootInputsBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all the output samples in the Process sequence that point to no input
    member this.LastSamples = 
        ProcessSequence.getFinalOutputsBy ((fun (io : IONode) -> io.IsSample),this)

    /// Returns the names of all sources in the Process sequence
    member this.Sources =
        ProcessSequence.getNodesBy ((fun (io : IONode) -> io.IsSource),this)

    /// Returns the names of all data in the Process sequence
    member this.Data =
        ProcessSequence.getNodesBy ((fun (io : IONode) -> io.IsFile),this)

    /// Returns the names of all the input data in the Process sequence to which no output points
    member this.FirstData = 
        ProcessSequence.getRootInputsBy ((fun (io : IONode) -> io.IsFile),this)

    /// Returns the names of all the output data in the Process sequence that point to no input
    member this.LastData = 
        ProcessSequence.getFinalOutputsBy ((fun (io : IONode) -> io.IsFile),this)

[<AttachMembers>]
type QLabProcess(node : LDNode) as this = 

   inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)
    do 
        if LDLabProcess.validate(node, context = context) |> not then
            failwithf "The provided node with id %s is not a valid Process" node.Id
        node.GetProperties(false)
        |> Seq.iter (fun kv ->
            if not (kv.Key = "Id") then
                this.SetProperty(kv.Key, kv.Value)
        )
        //node.DeepCopyPropertiesTo(this)

    static member name (labProcess : QLabProcess) =
        LDLabProcess.getNameAsString(labProcess, context = context)

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

    member this.Name = 
        QLabProcess.name this

    member this.Inputs = QLabProcess.inputs this

    member this.Outputs = QLabProcess.outputs this

    member this.InputNames = QLabProcess.inputNames this

    member this.OutputNames = QLabProcess.outputNames this

    member this.InputTypes = QLabProcess.inputTypes this

    member this.OutputTypes = QLabProcess.outputTypes this

    member this.ParameterValues =
        LDLabProcess.getParameterValues(this, graph = graph, context = context)
        |> ResizeArray.map (fun pvNode -> QPropertyValue(pvNode))

    member this.Protocol : QLabProtocol option = 
        LDLabProcess.tryGetExecutesLabProtocol(this, graph = graph, context = context)
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
type IONode(node : LDNode) as this =

    inherit LDNode(node.Id,node.SchemaType,node.AdditionalType)

    do        
        node.GetProperties(false)
        |> Seq.iter (fun kv ->
            if not (kv.Key = "Id") then
                this.SetProperty(kv.Key, kv.Value)
        )
        //node.DeepCopyPropertiesTo(this)

    member this.Name : string = LDSample.getNameAsString(this, context = context)

    member this.IsSample = LDSample.validateSample(this, context = context)

    member this.IsSource = LDSample.validateSource(this, context = context)

    member this.IsMaterial = LDSample.validateMaterial(this, context = context)

    member this.IsFile = LDFile.validate(this, context = context)

    member this.AdditionalProperties = 
        LDSample.getAdditionalProperties(this, context = context, graph = graph)
        |> ResizeArray.map (fun apNode -> QPropertyValue(apNode))

    member this.Characteristics = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsCharacteristic)

    member this.Factors = 
        this.AdditionalProperties
        |> ResizeArray.filter (fun pv -> pv.IsFactor)

    member this.HasResultOf() = 
       this.HasProperty(resultOf, context = context)

    member this.HasObjectOf() =
        this.HasProperty(objectOf, context = context)

    member this.ResultOf() = 
        this.GetPropertyNodes(resultOf, graph = graph, context = context)

    member this.ObjectOf() =
        this.GetPropertyNodes(objectOf, graph = graph, context = context)
        
    /// Returns all other nodes in the process sequence, that are connected to this node
    member this.GetNodes() = ProcessSequence.getNodesOfBy((fun _ -> true),this)

    /// Returns all other nodes in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstNodes() = ProcessSequence.getRootInputsOfBy((fun _ -> true),this)

    /// Returns all other nodes in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastNodes() = ProcessSequence.getFinalOutputsOfBy((fun _ -> true),this)

    /// Returns all other samples in the process sequence, that are connected to this node
    member this.GetSamples() = ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSample), this)

    /// Returns all other samples in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstSamples() = ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), this)
        
    /// Returns all other samples in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastSamples() = ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), this)

    /// Returns all other sources in the process sequence, that are connected to this node
    member this.GetSources() = ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSource), this)

    /// Returns all other data in the process sequence, that are connected to this node
    member this.GetData() = ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsFile), this)

    /// Returns all other data in the process sequence, that are connected to this node and have no more origin nodes pointing to them
    member this.GetFirstData() = ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), this)

    /// Returns all other data in the process sequence, that are connected to this node and have no more sink nodes they point to
    member this.GetLastData() = ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsFile), this)

    /// Returns all values in the process sequence, that are connected to this given node
    member this.GetValues() = ProcessSequence.getValuesOf(this)

    /// Returns all values in the process sequence, that are connected to this given node and come before it in the sequence
    member this.GetPreviousValues() = ProcessSequence.getPreviousValuesOf(this)

    /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingValues() = ProcessSequence.getSucceedingValuesOf(this)

    /// Returns all characteristic values in the process sequence, that are connected to the given node
    member this.GetCharacteristics() = ProcessSequence.getValuesOf(this).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousCharacteristics() = ProcessSequence.getPreviousValuesOf(this).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingCharacteristics() = ProcessSequence.getSucceedingValuesOf(this).Characteristics()

    /// Returns all parameter values in the process sequence, that are connected to the given node
    member this.GetParameters() = ProcessSequence.getValuesOf(this).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousParameters() = ProcessSequence.getPreviousValuesOf(this).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingParameters() = ProcessSequence.getSucceedingValuesOf(this).Parameters()

    /// Returns all factor values in the process sequence, that are connected to the given node
    member this.GetFactors() = ProcessSequence.getValuesOf(this).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousFactors() = ProcessSequence.getPreviousValuesOf(this).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingFactors() = ProcessSequence.getSucceedingValuesOf(this).Factors()

    /// Returns all component values in the process sequence, that are connected to the given node
    member this.GetComponents() = ProcessSequence.getValuesOf(this).Components()

    /// Returns all component values in the process sequence, that are connected to the given node and come before it in the sequence
    member this.GetPreviousComponents() = ProcessSequence.getPreviousValuesOf(this).Components()

    /// Returns all component values in the process sequence, that are connected to the given node and come after it in the sequence
    member this.GetSucceedingComponents() = ProcessSequence.getSucceedingValuesOf(this).Components()

    member this.GetProcesses() =
        ProcessSequence.getProcessesOf(this)

    member this.GetPreviousProcesses() =
        ProcessSequence.getPreviousProcessesOf(this)

    member this.GetSucceedingProcesses() =
        ProcessSequence.getSucceedingProcessesOf(this)

    override this.GetHashCode() =
        this.Id.GetHashCode()

    override this.Equals(obj) =
        match obj with
        | :? IONode as other -> this.Id = other.Id
        | _ -> false