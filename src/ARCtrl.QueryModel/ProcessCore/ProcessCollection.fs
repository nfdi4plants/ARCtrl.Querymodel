namespace rec ARCtrl.QueryModel.ProcessCore

open ARCtrl
open ARCtrl.ROCrate
open ARCtrl.QueryModel
open System.Text.Json.Serialization
open System.Text.Json
open System.IO

open System.Collections.Generic
open System.Collections


/// Type representing a queryable collection of processes, which model the experimental graph
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
        let ps = ProcessSequence.getProcesses(?ps = ps)
        node.ResultOf()
        |> Seq.exists (fun p -> ps.HasProcess p.Id)
        |> not

    static member nodeIsFinal (node : IONode, ?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        node.ObjectOf()
        |> Seq.exists (fun p -> ps.HasProcess p.Id)
        |> not


    /// Returns the list of all nodes (sources, samples, data) in the ProcessSequence
    static member getNodes (?ps : ProcessSequence) =
        match ps with
        | Some ps -> 
            ProcessSequence.getProcesses(ps = ps).Processes        
            |> ResizeArray.collect (fun p -> 
                [p.Input; p.Output]
            )
            |> ResizeArray.distinct     
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
                        |> ResizeArray.collect (fun pn ->
                            if ps.HasProcess(pn.Id) then
                                let p = ps.GetProcess(pn.Id)
                                f p
                                |> ResizeArray.iter (fun r -> result.Add r |> ignore)
                                ResizeArray [p.Input]
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
                        |> ResizeArray.collect (fun pn ->
                            if ps.HasProcess(pn.Id) then
                                let p = ps.GetProcess(pn.Id)
                                f p
                                |> ResizeArray.iter (fun r -> result.Add r |> ignore)
                                ResizeArray [p.Output]
                            else 
                                ResizeArray []
                        )
                )
                |> loop
        loop (ResizeArray [node])

    /// Returns a new process sequence, only with those rows that contain either an educt or a product entity of the given node (or entity)
    static member getSubTreeOf (node : string, ?ps : ProcessSequence) =
        let forwardProcesses = 
            ProcessSequence.collectForwards(IONode(graph.GetNode(node)), id >> ResizeArray.singleton, ?ps = ps)
        let backwardProcesses = 
            ProcessSequence.collectBackwards(IONode(graph.GetNode(node)), id >> ResizeArray.singleton, ?ps = ps)
        ResizeArray.append forwardProcesses backwardProcesses
        |> ResizeArray.distinct
        |> ProcessSequence

    /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints
    static member getRootInputs (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        ProcessSequence.getNodes(ps = ps)
        |> ResizeArray.filter(fun n ->
            ProcessSequence.nodeIsRoot(n, ps = ps)
        )

    /// Returns the names of all final outputs of the processSequence, which point to no further nodes
    static member getFinalOutputs (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        ProcessSequence.getNodes(ps = ps)
        |> ResizeArray.filter(fun n ->
            ProcessSequence.nodeIsFinal(n, ps = ps)
        )

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
        ProcessSequence.collectBackwards (node, QLabProcess.input>>ResizeArray.singleton, ?ps = ps)

    static member getSucceedingNodesOf (node : IONode, ?ps : ProcessSequence) =
        ProcessSequence.collectForwards (node, QLabProcess.output>>ResizeArray.singleton, ?ps = ps)

    /// Returns the names of all nodes processSequence, which are connected to the given node and for which the predicate returns true
    static member getNodesOfBy (predicate : IONode -> bool, node : string, ?ps : ProcessSequence) =
        ProcessSequence.getSubTreeOf(node,?ps = ps)
        |> fun ps -> ProcessSequence.getNodesBy(predicate,ps)

    /// Returns the initial inputs final outputs of the assay, to which no processPoints, which are connected to the given node and for which the predicate returns true
    static member getRootInputsOfBy (predicate : IONode -> bool, node : string, ?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        let f (p : QLabProcess) =
            let input = QLabProcess.input p
            if ProcessSequence.nodeIsRoot(input, ps) && predicate input then
                ResizeArray.singleton input
            else
                ResizeArray()
        ProcessSequence.collectBackwards(IONode(graph.GetNode(node)), f, ps = ps)

    /// Returns the final outputs of the assay, which point to no further nodes, which are connected to the given node and for which the predicate returns true
    static member getFinalOutputsOfBy (predicate : IONode -> bool, node : string, ?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        let f (p : QLabProcess) =
            let output = QLabProcess.output p
            if ProcessSequence.nodeIsFinal(output, ps) && predicate output then
                ResizeArray.singleton output
            else
                ResizeArray()
        ProcessSequence.collectForwards(IONode(graph.GetNode(node)), f, ps = ps)
       
    static member getValues (?ps : ProcessSequence) =
        let ps = ProcessSequence.getProcesses(?ps = ps)
        ps.Processes
        |> ResizeArray.collect (fun p -> 
            ResizeArray [
                yield! p.Input.Characteristics
                yield! p.ParameterValues
                yield! p.Components
                yield! p.Output.Factors            
            ]
        )
        |> QValueCollection

    /// Returns the previous values of the given node
    static member getPreviousValuesOf (node : string, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                ResizeArray [
                    yield! p.Input.Characteristics
                    yield! p.ParameterValues
                    yield! p.Output.Factors            
                ]
        ProcessSequence.collectBackwards(IONode(graph.GetNode(node)), f, ?ps = ps)
        |> QValueCollection

    /// Returns the succeeding values of the given node
    static member getSucceedingValuesOf (node : string, ?protocolName : string, ?ps : ProcessSequence) =
        let f (p : QLabProcess) =
            match protocolName with
            | Some pn when p.Protocol.IsSome && p.Protocol.Value.Name <> pn ->
                ResizeArray []
            | _ ->
                ResizeArray [
                    yield! p.Input.Characteristics
                    yield! p.ParameterValues
                    yield! p.Components
                    yield! p.Output.Factors            
                ]
        ProcessSequence.collectForwards(IONode(graph.GetNode(node)), f, ?ps = ps)
        |> QValueCollection

    /// Returns a new ProcessSequence, with only the values from the processes that implement the given protocol
    static member onlyValuesOfProtocol (protocolName : string, ?ps : ProcessSequence) : QValueCollection =
        ProcessSequence.getProcesses(?ps = ps).Processes
        |> ResizeArray.collect (fun p -> 
            match p.Protocol with
            | Some pt when pt.Name = protocolName ->
                ResizeArray [
                    yield! p.Input.Characteristics
                    yield! p.ParameterValues
                    yield! p.Components
                    yield! p.Output.Factors            
                ]
            | _ -> ResizeArray []
        )
        |> QValueCollection

    /// Returns the names of all nodes in the Process sequence
    member this.NodesOf(node : IONode) =
        ProcessSequence.getNodesOfBy((fun _ -> true),node.Name,this)

        /// Returns the names of all nodes in the Process sequence
    member this.NodesOf(node) =
        ProcessSequence.getNodesOfBy((fun _ -> true),node,this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
    member this.FirstNodesOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun _ -> true),node.Name,this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
    member this.LastNodesOf(node : IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun _ -> true),node.Name,this)

    /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
    member this.FirstNodesOf(node) = 
        ProcessSequence.getRootInputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
    member this.LastNodesOf(node) = 
        ProcessSequence.getRootInputsOfBy ((fun _ -> true),node,this)

    /// Returns the names of all samples in the Process sequence, that are connected to the given node
    member this.SamplesOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSample), node.Name, this)

        /// Returns the names of all samples in the Process sequence, that are connected to the given node
    member this.SamplesOf(node) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
    member this.FirstSamplesOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), node.Name, this)

    /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
    member this.LastSamplesOf(node : IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), node.Name, this)

    /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
    member this.FirstSamplesOf(node) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
    member this.LastSamplesOf(node) = 
        ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsSample), node, this)

    /// Returns the names of all sources in the Process sequence, that are connected to the given node
    member this.SourcesOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSource), node.Name, this)

    /// Returns the names of all sources in the Process sequence, that are connected to the given node
    member this.SourcesOf(node) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsSource), node, this)

    /// Returns the names of all data in the Process sequence, that are connected to the given node
    member this.DataOf(node : IONode) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsFile), node.Name, this)

    /// Returns the names of all data in the Process sequence, that are connected to the given node
    member this.DataOf(node) =
        ProcessSequence.getNodesOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
    member this.FirstDataOf(node : IONode) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), node.Name, this)

    /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
    member this.LastDataOf(node : IONode) = 
        ProcessSequence.getFinalOutputsOfBy ((fun (io : IONode) -> io.IsFile), node.Name, this)

    /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
    member this.FirstDataOf(node) = 
        ProcessSequence.getRootInputsOfBy ((fun (io : IONode) -> io.IsFile), node, this)

    /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
    member this.LastDataOf(node) = 
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


    member this.PreviousValuesOf(node : string, ?protocolName) =
        ProcessSequence.getPreviousValuesOf(node, ?protocolName = protocolName, ps = this)

    member this.SucceedingValuesOf(node : string, ?protocolName) =
        ProcessSequence.getSucceedingValuesOf(node, ?protocolName = protocolName, ps = this)

    /// Returns all values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousValuesOf(node : IONode, ?protocolName) =
        this.PreviousValuesOf(node.Name, ?protocolName = protocolName)   

    /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingValuesOf(node : IONode, ?protocolName) =
        this.SucceedingValuesOf(node.Name, ?protocolName = protocolName)

    /// Returns all values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ValuesOf(node : string, ?protocolName : string) =
        ResizeArray.append (this.PreviousValuesOf(node,?protocolName = protocolName).Values) (this.SucceedingValuesOf(node,?protocolName = protocolName).Values)
        |> QValueCollection

    /// Returns all values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ValuesOf(node : IONode, ?protocolName : string) =
        this.ValuesOf(node.Name, ?protocolName = protocolName)

    /// Returns all characteristic values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.CharacteristicsOf(node : string, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.CharacteristicsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousCharacteristicsOf(node : string, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousCharacteristicsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingCharacteristicsOf(node : string, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingCharacteristicsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Characteristics()

    /// Returns all parameter values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ParametersOf(node : string, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ParametersOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousParametersOf(node : string, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousParametersOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingParametersOf(node : string, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingParametersOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Parameters()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.FactorsOf(node : string, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.FactorsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousFactorsOf(node : string, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousFactorsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol and come after it in the sequence
    member this.SucceedingFactorsOf(node : string, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all factor values in the process sequence, that are connected to the given node 
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol and come after it in the sequence
    member this.SucceedingFactorsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Factors()

    /// Returns all components values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ComponentsOf(node : string, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.ComponentsOf(node : IONode, ?protocolName) =
            this.ValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousComponentsOf(node : string, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come before it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.PreviousComponentsOf(node : IONode, ?protocolName) =
            this.PreviousValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingComponentsOf(node : string, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Components()

    /// Returns all components values in the process sequence, that are connected to the given node and come after it in the sequence
    ///
    /// If a protocol name is given, returns only the values of the processes that implement this protocol
    member this.SucceedingComponentsOf(node : IONode, ?protocolName) =
            this.SucceedingValuesOf(node,?protocolName = protocolName).Components()

    member this.Contains(ontology : OntologyAnnotation, ?protocolName) = 
            this.Values(?protocolName = protocolName).Contains ontology

    member this.Contains(name : string, ?protocolName) = 
            this.Values(?protocolName = protocolName).Contains name

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

///// One Node of an ISA Process Sequence (Source, Sample, Data)
//type IONode(name : string, ioType : IOType, ?parentProcessSequence : ProcessSequence) =
    
//    member this.DataContext = 
//        if ioType.isData then 
//            match DataContext.tryGetDataContextByName name with
//            | Some dc -> 
//                DataContext(?id = dc.ID, name = name, ?format = dc.Format, ?selectorFormat = dc.SelectorFormat, ?explication = dc.Explication, ?unit = dc.Unit, ?objectType = dc.ObjectType, ?label = dc.Label, ?description = dc.Description, ?generatedBy = dc.GeneratedBy, comments = dc.Comments)
//                |> Some
//            | None -> Some (DataContext(name = name))
//        else 
//            None

//    member this.FilePath = 
//        if ioType.isData then 
//            this.DataContext.Value.FilePath.Value
//        else 
//            ""

//    member this.Selector = 
//        if ioType.isData then 
//            this.DataContext.Value.Selector
//        else 
//            None

//    /// Returns the process sequence in which the node appears
//    member this.ParentProcessSequence = parentProcessSequence |> Option.defaultValue (ProcessSequence(ResizeArray []))

//    /// Identifying name of the node
//    member this.Name = name

//    /// Type of node (source, sample, data, raw data ...)
//    member this.IOType : IOType = ioType

//    interface System.IEquatable<IONode> with
//        member this.Equals other = other.Name.Equals this.Name

//    override this.Equals other =
//        match other with
//        | :? IONode as p -> (this :> System.IEquatable<_>).Equals p
//        | _ -> false

//    override this.GetHashCode () = this.Name.GetHashCode()

//    interface System.IComparable with
//        member this.CompareTo other =
//            match other with
//            | :? IONode as p -> (this :> System.IComparable<_>).CompareTo p
//            | _ -> -1

//    interface System.IComparable<IONode> with
//        member this.CompareTo other = other.Name.CompareTo this.Name

//    /// Returns true, if the node is a source
//    member this.isSource = this.IOType.isSource

//    /// Returns true, if the node is a sample
//    member this.isSample = this.IOType.isSample
    
//    /// Returns true, if the node is a data
//    member this.isData = this.IOType.isData

//    /// Returns true, if the node is a material
//    member this.isMaterial = this.IOType.isMaterial


//[<AutoOpen>]
//module IONodeExtensions =

//    type IONode with

//        /// Returns all other nodes in the process sequence, that are connected to this node
//        member this.Nodes = this.ParentProcessSequence.NodesOf(this)

//        /// Returns all other nodes in the process sequence, that are connected to this node and have no more origin nodes pointing to them
//        member this.FirstNodes = this.ParentProcessSequence.FirstNodesOf(this)

//        /// Returns all other nodes in the process sequence, that are connected to this node and have no more sink nodes they point to
//        member this.LastNodes = this.ParentProcessSequence.LastNodesOf(this)

//        /// Returns all other samples in the process sequence, that are connected to this node
//        member this.Samples = this.ParentProcessSequence.SamplesOf(this)

//        /// Returns all other samples in the process sequence, that are connected to this node and have no more origin nodes pointing to them
//        member this.FirstSamples = this.ParentProcessSequence.FirstSamplesOf(this)
        
//        /// Returns all other samples in the process sequence, that are connected to this node and have no more sink nodes they point to
//        member this.LastSamples = this.ParentProcessSequence.LastSamplesOf(this)

//        /// Returns all other sources in the process sequence, that are connected to this node
//        member this.Sources = this.ParentProcessSequence.SourcesOf(this)

//        /// Returns all other data in the process sequence, that are connected to this node
//        member this.Data = this.ParentProcessSequence.FirstDataOf(this)

//        /// Returns all other data in the process sequence, that are connected to this node and have no more origin nodes pointing to them
//        member this.FirstData = this.ParentProcessSequence.FirstDataOf(this)

//        /// Returns all other data in the process sequence, that are connected to this node and have no more sink nodes they point to
//        member this.LastData = this.ParentProcessSequence.LastNodesOf(this)

//        /// Returns all values in the process sequence, that are connected to this given node
//        member this.Values = this.ParentProcessSequence.ValuesOf(this)

//        /// Returns all values in the process sequence, that are connected to this given node and come before it in the sequence
//        member this.PreviousValues = this.ParentProcessSequence.PreviousValuesOf(this)

//        /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
//        member this.SucceedingValues = this.ParentProcessSequence.SucceedingValuesOf(this)

//        /// Returns all characteristic values in the process sequence, that are connected to the given node
//        member this.Characteristics = this.ParentProcessSequence.CharacteristicsOf(this)

//        /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
//        member this.PreviousCharacteristics = this.ParentProcessSequence.PreviousCharacteristicsOf(this)

//        /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
//        member this.SucceedingCharacteristics = this.ParentProcessSequence.SucceedingCharacteristicsOf(this)

//        /// Returns all parameter values in the process sequence, that are connected to the given node
//        member this.Parameters = this.ParentProcessSequence.ParametersOf(this)

//        /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
//        member this.PreviousParameters = this.ParentProcessSequence.PreviousParametersOf(this)

//        /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
//        member this.SucceedingParameters = this.ParentProcessSequence.SucceedingParametersOf(this)

//        /// Returns all factor values in the process sequence, that are connected to the given node
//        member this.Factors = this.ParentProcessSequence.FactorsOf(this)

//        /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
//        member this.PreviousFactors = this.ParentProcessSequence.PreviousFactorsOf(this)

//        /// Returns all factor values in the process sequence, that are connected to the given node and come after it in the sequence
//        member this.SucceedingFactors = this.ParentProcessSequence.SucceedingFactorsOf(this)

//        /// Returns all component values in the process sequence, that are connected to the given node
//        member this.Components = this.ParentProcessSequence.ComponentsOf(this)

//        /// Returns all component values in the process sequence, that are connected to the given node and come before it in the sequence
//        member this.PreviousComponents = this.ParentProcessSequence.PreviousComponentsOf(this)

//        /// Returns all component values in the process sequence, that are connected to the given node and come after it in the sequence
//        member this.SucceedingComponents = this.ParentProcessSequence.SucceedingComponentsOf(this)
