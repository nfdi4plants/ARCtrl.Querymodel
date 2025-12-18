namespace rec ARCtrl.QueryModel

open ARCtrl
open System.IO

open System.Collections.Generic
open System.Collections



[<AutoOpen>]
module ArcTables = 

    type IOType with
        member this.isSource = match this with | IOType.Source -> true | _ -> false

        member this.isSample = match this with | IOType.Sample -> true | _ -> false

        member this.isMaterial = match this with | IOType.Material -> true | _ -> false

        member this.isData = match this with | IOType.Data -> true | _ -> false


    /// Type representing a queryable collection of processes, which model the experimental graph
    type ArcTables with
       
        static member concat (pss : #IEnumerable<#IEnumerable<ArcTable>>) =
            Seq.concat pss
            |> ResizeArray
            |> ArcTables 

        //member this.TryGetChildProtocolOf(parentProtocolType : OntologyAnnotation) =
        //    this.Sheets
        //    |> List.collect (fun s -> s.Protocols)
        //    |> List.choose (fun p -> if p.IsChildProtocolTypeOf(parentProtocolType) then Some p else None)
        //    |> Option.fromValueWithDefault []

        //member this.TryGetChildProtocolOf(parentProtocolType : OntologyAnnotation, obo : Obo.OboOntology) =
        //    this.Sheets
        //    |> List.collect (fun s -> s.Protocols)
        //    |> List.choose (fun p -> if p.IsChildProtocolTypeOf(parentProtocolType, obo) then Some p else None)
        //    |> Option.fromValueWithDefault []

        /// Returns the list of all nodes (sources, samples, data) in the ProcessSequence
        static member getNodes (ps : #ArcTables) =
            ps.Tables 
            |> Seq.collect (fun p -> 
                p.Rows 
                |> Seq.collect (fun r -> 
                    [
                        QNode(r.InputName,r.InputType,ps)
                        QNode(r.OutputName,r.OutputType,ps)
                    ]
                )
            )
            |> Seq.toList
            |> List.distinct     

        /// Returns a new process sequence, only with those rows that contain either an educt or a product entity of the given node (or entity)
        static member getSubTreeOf (node : string) (ps : #ArcTables) =
            let rec collectForwardNodes nodes =
                let newNodes = 
                    ps.Tables
                    |> Seq.collect (fun sheet ->
                        sheet.Rows 
                        |> Seq.choose (fun r -> if List.contains r.InputName nodes then Some r.OutputName else None)
                    )
                    |> Seq.toList
                    |> List.append nodes 
                    |> List.distinct
                
                if newNodes = nodes then nodes
                else collectForwardNodes newNodes

            let rec collectBackwardNodes nodes =
                let newNodes = 
                    ps.Tables
                    |> Seq.collect (fun sheet ->
                        sheet.Rows 
                        |> Seq.choose (fun r -> if List.contains r.Output nodes then Some r.Input else None)
                    )
                    |> Seq.toList
                    |> List.append nodes 
                    |> List.distinct
                       
                if newNodes = nodes then nodes
                else collectBackwardNodes newNodes

            let forwardNodes = collectForwardNodes [node]
            let backwardNodes = collectBackwardNodes [node]

            let arcTables = ps |> Seq.map (fun t -> t.Copy()) |> Seq.toList |> ResizeArray

            arcTables
            |> ResizeArray.map (fun t ->
                (t.GetOutputColumn().Cells)
                |> Seq.zip (t.GetInputColumn().Cells)
                |> Seq.indexed 
                |> Seq.choose (fun (i,(inp,out)) ->
                    if Seq.contains inp.AsFreeText forwardNodes || (Seq.contains out.AsFreeText backwardNodes) then
                        None
                    else Some i                              
                )
                |> Seq.toArray
                |> t.RemoveRows
                t
            )
            |> ArcTables

        /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints
        static member getRootInputs (ps : #ArcTables) =
            let inputs = ps.Tables |> ResizeArray.collect (fun p -> p.Rows |> List.map (fun r -> r.Input,r.InputType))
            let outputs =  ps.Tables |> ResizeArray.collect (fun p -> p.Rows |> List.map (fun r -> r.Output)) |> Set.ofSeq
            inputs
            |> ResizeArray.choose (fun (iname,it) -> 
                if outputs.Contains iname then
                    None
                else 
                    QNode(iname,it,ps)
                    |> Some
                )

        /// Returns the names of all final outputs of the processSequence, which point to no further nodes
        static member getFinalOutputs (ps : #ArcTables) =
            let inputs = ps.Tables |> ResizeArray.collect (fun p -> p.Rows |> List.map (fun r -> r.Input)) |> Set.ofSeq
            let outputs =  ps.Tables |> ResizeArray.collect (fun p -> p.Rows |> List.map (fun r -> r.Output, r.OutputType))
            outputs
            |> ResizeArray.choose (fun (oname,ot) -> 
                if inputs.Contains oname then
                    None
                else 
                    QNode(oname,ot,ps)
                    |> Some
                )
            |> ResizeArray.distinctBy (fun n -> n.Name)

        /// Returns the names of all nodes for which the predicate reutrns true
        static member getNodesBy (predicate : IOType -> bool) (ps : #ArcTables) =
            ps.Tables 
            |> ResizeArray.collect (fun p -> 
                p.Rows 
                |> List.collect (fun r -> 
                    [                   
                        if predicate r.InputType then QNode(r.Input, r.InputType, ps); 
                        if predicate r.OutputType then  QNode(r.Output, r.InputType, ps)
                    ])
            )
            |> ResizeArray.distinct 

        /// Returns the names of all initial inputs final outputs of the processSequence, to which no processPoints, and for which the predicate returns true
        static member getRootInputsBy (predicate : IOType -> bool) (ps : #ArcTables) =
            let mappings = 
                ps.Tables 
                |> ResizeArray.collect (fun p -> 
                    p.Rows 
                    |> List.map (fun r -> QNode(r.Input,r.InputType,ps), QNode(r.Output,r.OutputType,ps))
                    |> List.distinct
                ) 
                |> Seq.toList
                |> List.groupBy fst 
                |> List.map (fun (out,ins) -> out, ins |> List.map snd)
                |> Map.ofList

            let predicate (entity : QNode) =
                predicate entity.IOType

            let rec loop (searchEntities : QNode list) (foundEntities : QNode list) = 
                if searchEntities.IsEmpty then foundEntities |> List.distinct
                else
                    let targs = searchEntities |> List.filter predicate
                    let nonTargs = searchEntities |> List.filter (predicate >> not)
                    let nextSearchEntities = nonTargs |> List.collect (fun en -> Map.tryFind en mappings |> Option.defaultValue [])
                    loop nextSearchEntities targs

            loop (ArcTables.getRootInputs ps |> List.ofSeq) []

        /// Returns the names of all final outputs of the processSequence, which point to no further nodes, and for which the predicate returns true
        static member getFinalOutputsBy (predicate : IOType -> bool) (ps : #ArcTables) =
            let mappings = 
                ps.Tables 
                |> ResizeArray.collect (fun p -> 
                    p.Rows 
                    |> List.map (fun r -> QNode(r.Output,r.OutputType,ps), QNode(r.Input,r.InputType,ps))
                    |> List.distinct
                ) 
                |> Seq.toList
                |> List.groupBy fst 
                |> List.map (fun (out,ins) -> out, ins |> List.map snd)
                |> Map.ofList  

            let predicate (entity : QNode) =
                predicate entity.IOType


            let rec loop (searchEntities : QNode list) (foundEntities : QNode list) = 
                if searchEntities.IsEmpty then foundEntities |> List.distinct
                else
                    let targs = searchEntities |> List.filter predicate
                    let nonTargs = searchEntities |> List.filter (predicate >> not)
                    let nextSearchEntities = nonTargs |> List.collect (fun en -> Map.tryFind en mappings |> Option.defaultValue [])
                    loop nextSearchEntities targs

            loop (ArcTables.getFinalOutputs ps |> List.ofSeq) []

        static member getPreviousNodesBy (node : string) (ps : #ArcTables) =

            let rec collectBackwardNodes nodes =
                let newNodes = 
                    ps.Tables
                    |> Seq.collect (fun sheet ->
                        sheet.Rows 
                        |> Seq.choose (fun r -> if List.contains r.Output nodes then Some r.Input else None)
                    )
                    |> Seq.toList
                    |> List.append nodes 
                    |> List.distinct
                       
                if newNodes = nodes then nodes
                else collectBackwardNodes newNodes

            collectBackwardNodes [node]

        static member getSucceedingNodesBy (node : string) (ps : #ArcTables) =
            let rec collectForwardNodes nodes =
                let newNodes = 
                    ps.Tables
                    |> Seq.collect (fun sheet ->
                        sheet.Rows 
                        |> Seq.choose (fun r -> if List.contains r.InputName nodes then Some r.OutputName else None)
                    )
                    |> Seq.toList
                    |> List.append nodes 
                    |> List.distinct
                
                if newNodes = nodes then nodes
                else collectForwardNodes newNodes
            collectForwardNodes [node]

        /// Returns the names of all nodes processSequence, which are connected to the given node and for which the predicate returns true
        static member getNodesOfBy (predicate : IOType -> bool) (node : string) (ps : #ArcTables) =
            ArcTables.getSubTreeOf node ps
            |> ArcTables.getNodesBy predicate

        /// Returns the initial inputs final outputs of the assay, to which no processPoints, which are connected to the given node and for which the predicate returns true
        static member getRootInputsOfBy (predicate : IOType -> bool) (node : string) (ps : #ArcTables) =
            ArcTables.getSubTreeOf node ps
            |> ArcTables.getRootInputsBy predicate

        /// Returns the final outputs of the assay, which point to no further nodes, which are connected to the given node and for which the predicate returns true
        static member getFinalOutputsOfBy (predicate : IOType -> bool) (node : string) (ps : #ArcTables) =
            ArcTables.getSubTreeOf node ps
            |> ArcTables.getFinalOutputsBy predicate
       
        /// Returns the previous values of the given node
        static member getPreviousValuesOf (ps : #ArcTables) (node : string) =
            let mappings = 
                ps.Tables 
                |> ResizeArray.collect (fun p -> 
                    p.Rows 
                    |> List.groupBy (fun r -> r.Output)
                    //|> List.map (fun (o,rs) -> o, rs |> List.map snd)
                ) 
                |> Map.ofSeq
            let rec loop values lastState state = 
                if lastState = state then values 
                else
                    let newState,newValues = 
                        state 
                        |> List.map (fun s -> 
                            mappings.TryFind s 
                            |> Option.map (fun rs -> 
                                rs 
                                |> List.map (fun r -> r.Input,r.Values |> Seq.toList)
                                |> List.unzip
                                |> fun (s,vs) -> s, vs |> List.concat
                            )                            
                            |> Option.defaultValue ([],[])
                        )
                        |> List.unzip
                        |> fun (s,vs) -> s |> List.concat, vs |> List.concat
                    loop (newValues@values) state newState
            loop [] [] [node]  
            |> ValueCollection

        /// Returns the succeeding values of the given node
        static member getSucceedingValuesOf (ps : #ArcTables) (sample : string) =
            let mappings = 
                ps.Tables 
                |> ResizeArray.collect (fun p -> 
                    p.Rows 
                    |> List.groupBy (fun r -> r.Input)
                ) 

                |> Map.ofSeq
            let rec loop values lastState state = 
                if lastState = state then values 
                else
                    let newState,newValues = 
                        state 
                        |> List.map (fun s -> 
                            mappings.TryFind s 
                            |> Option.map (fun rs -> 
                                rs 
                                |> List.map (fun r -> r.Output,r.Values |> Seq.toList)
                                |> List.unzip
                                |> fun (s,vs) -> s, vs |> List.concat
                            )                            
                            |> Option.defaultValue ([],[])
                        )
                        |> List.unzip
                        |> fun (s,vs) -> s |> List.concat, vs |> List.concat
                    loop (values@newValues) state newState
            loop [] [] [sample]
            |> ValueCollection

        /// Returns a new ProcessSequence, with only the values from the processes that implement the given protocol
        static member onlyValuesOfProtocol (ps : #ArcTables) (protocolName : string) =
            
            ps.Tables
            |> ResizeArray.filter (fun t -> 
                t.Name = protocolName

                //if s.Name = pn then 
                //    s
                //else 
                //    {s with Rows = s.Rows |> List.map (fun r -> {r with Vals = []})}
            )
            |> ArcTables                    

        /// Returns the names of all nodes in the Process sequence
        member this.NodesOf(node : QNode) =
            ArcTables.getNodesOfBy (fun _ -> true) node.Name this

            /// Returns the names of all nodes in the Process sequence
        member this.NodesOf(node) =
            ArcTables.getNodesOfBy (fun _ -> true) node this

            /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
        member this.FirstNodesOf(node : QNode) = 
            ArcTables.getRootInputsOfBy (fun _ -> true) node.Name this

        /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
        member this.LastNodesOf(node : QNode) = 
            ArcTables.getFinalOutputsOfBy (fun _ -> true) node.Name this

        /// Returns the names of all the input nodes in the Process sequence to which no output points, that are connected to the given node
        member this.FirstNodesOf(node) = 
            ArcTables.getRootInputsOfBy (fun _ -> true) node this

        /// Returns the names of all the output nodes in the Process sequence that point to no input, that are connected to the given node
        member this.LastNodesOf(node) = 
            ArcTables.getFinalOutputsOfBy (fun _ -> true) node this

        /// Returns the names of all samples in the Process sequence, that are connected to the given node
        member this.SamplesOf(node : QNode) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isSample) node.Name this

            /// Returns the names of all samples in the Process sequence, that are connected to the given node
        member this.SamplesOf(node) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isSample) node this

        /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
        member this.FirstSamplesOf(node : QNode) = 
            ArcTables.getRootInputsOfBy (fun (io : IOType) -> io.isSample) node.Name this

        /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
        member this.LastSamplesOf(node : QNode) = 
            ArcTables.getFinalOutputsOfBy (fun (io : IOType) -> io.isSample) node.Name this

        /// Returns the names of all the input samples in the Process sequence to which no output points, that are connected to the given node
        member this.FirstSamplesOf(node) = 
            ArcTables.getRootInputsOfBy (fun (io : IOType) -> io.isSample) node this

        /// Returns the names of all the output samples in the Process sequence that point to no input, that are connected to the given node
        member this.LastSamplesOf(node) = 
            ArcTables.getFinalOutputsOfBy (fun (io : IOType) -> io.isSample) node this

        /// Returns the names of all sources in the Process sequence, that are connected to the given node
        member this.SourcesOf(node : QNode) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isSource) node.Name this

        /// Returns the names of all sources in the Process sequence, that are connected to the given node
        member this.SourcesOf(node) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isSource) node this

        /// Returns the names of all data in the Process sequence, that are connected to the given node
        member this.DataOf(node : QNode) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isData) node.Name this

        /// Returns the names of all data in the Process sequence, that are connected to the given node
        member this.DataOf(node) =
            ArcTables.getNodesOfBy (fun (io : IOType) -> io.isData) node this

        /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
        member this.FirstDataOf(node : QNode) = 
            ArcTables.getRootInputsOfBy (fun (io : IOType) -> io.isData) node.Name this

        /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
        member this.LastDataOf(node : QNode) = 
            ArcTables.getFinalOutputsOfBy (fun (io : IOType) -> io.isData) node.Name this

        /// Returns the names of all the input data in the Process sequence to which no output points, that are connected to the given node
        member this.FirstDataOf(node) = 
            ArcTables.getRootInputsOfBy (fun (io : IOType) -> io.isData) node this

        /// Returns the names of all the output data in the Process sequence that point to no input, that are connected to the given node
        member this.LastDataOf(node) = 
            ArcTables.getFinalOutputsOfBy (fun (io : IOType) -> io.isData) node this

        /// Returns all values in the process sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Values(?ProtocolName) = 
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Tables
            |> ResizeArray.collect (fun s ->
                s.ISAValues.Values().Values
            )
            |> ValueCollection

        /// Returns all values in the process sequence whose header matches the given category
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Values(ontology : OntologyAnnotation, ?ProtocolName) = 
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Tables
            |> ResizeArray.collect (fun s -> s.ISAValues.Values().WithCategory(ontology).Values)
            |> ValueCollection

        /// Returns all values in the process sequence whose header matches the given name
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Values(name : string, ?ProtocolName) = 
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Tables
            |> ResizeArray.collect (fun s -> s.ISAValues.Values().WithName(name).Values)
            |> ValueCollection

        /// Returns all factor values in the process sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Factors(?ProtocolName) =
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Values().Factors()

        /// Returns all parameter values in the process sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Parameters(?ProtocolName) =
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Values().Parameters()

        /// Returns all characteristic values in the process sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Characteristics(?ProtocolName) =
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Values().Characteristics()

        /// Returns all components in the process sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.Components(?ProtocolName) =
            let ps = if ProtocolName.IsSome then ArcTables.onlyValuesOfProtocol this ProtocolName.Value else this
            ps.Values().Components()


        member this.PreviousValuesOf(node : string, ?ProtocolName) =
            match ProtocolName with
            | Some ps ->
                let previousNodes = ArcTables.getPreviousNodesBy node this
                let ps = ArcTables.onlyValuesOfProtocol this ps
                previousNodes
                |> Seq.collect (fun n -> 
                    ps.PreviousValuesOf(n)
                )                 
                |> ValueCollection
            | None ->
                ArcTables.getPreviousValuesOf this node

        member this.SucceedingValuesOf(node : string, ?ProtocolName) =
            match ProtocolName with
            | Some ps ->
                let succeedingNodes = ArcTables.getSucceedingNodesBy node this
                let ps = ArcTables.onlyValuesOfProtocol this ps
                succeedingNodes
                |> Seq.collect (fun n -> 
                    ps.SucceedingValuesOf(n)
                ) 
                |> ValueCollection
            | None ->
                ArcTables.getSucceedingValuesOf this node

        /// Returns all values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousValuesOf(node : QNode, ?ProtocolName) =
            this.PreviousValuesOf(node.Name, ?ProtocolName = ProtocolName)   

        /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingValuesOf(node : QNode, ?ProtocolName) =
            this.SucceedingValuesOf(node.Name, ?ProtocolName = ProtocolName)

        /// Returns all values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ValuesOf(node : string, ?ProtocolName : string) =
            this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Values @ this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Values
            |> ValueCollection

        /// Returns all values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ValuesOf(node : QNode, ?ProtocolName : string) =
            this.ValuesOf(node.Name, ?ProtocolName = ProtocolName)

        /// Returns all characteristic values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.CharacteristicsOf(node : string, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all characteristic values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.CharacteristicsOf(node : QNode, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousCharacteristicsOf(node : string, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousCharacteristicsOf(node : QNode, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingCharacteristicsOf(node : string, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingCharacteristicsOf(node : QNode, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Characteristics()

        /// Returns all parameter values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ParametersOf(node : string, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all parameter values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ParametersOf(node : QNode, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousParametersOf(node : string, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousParametersOf(node : QNode, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingParametersOf(node : string, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingParametersOf(node : QNode, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Parameters()

        /// Returns all factor values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.FactorsOf(node : string, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all factor values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.FactorsOf(node : QNode, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousFactorsOf(node : string, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousFactorsOf(node : QNode, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all factor values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol and come after it in the sequence
        member this.SucceedingFactorsOf(node : string, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all factor values in the process sequence, that are connected to the given node 
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol and come after it in the sequence
        member this.SucceedingFactorsOf(node : QNode, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Factors()

        /// Returns all components values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ComponentsOf(node : string, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Components()

        /// Returns all components values in the process sequence, that are connected to the given node
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.ComponentsOf(node : QNode, ?ProtocolName) =
             this.ValuesOf(node,?ProtocolName = ProtocolName).Components()

        /// Returns all components values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousComponentsOf(node : string, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Components()

        /// Returns all components values in the process sequence, that are connected to the given node and come before it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.PreviousComponentsOf(node : QNode, ?ProtocolName) =
             this.PreviousValuesOf(node,?ProtocolName = ProtocolName).Components()

        /// Returns all components values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingComponentsOf(node : string, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Components()

        /// Returns all components values in the process sequence, that are connected to the given node and come after it in the sequence
        ///
        /// If a protocol name is given, returns only the values of the processes that implement this protocol
        member this.SucceedingComponentsOf(node : QNode, ?ProtocolName) =
             this.SucceedingValuesOf(node,?ProtocolName = ProtocolName).Components()

        member this.Contains(ontology : OntologyAnnotation, ?ProtocolName) = 
             this.Values(?ProtocolName = ProtocolName).Contains ontology

        member this.Contains(name : string, ?ProtocolName) = 
             this.Values(?ProtocolName = ProtocolName).Contains name

        /// Returns the names of all nodes in the Process sequence
        member this.Nodes =
            ArcTables.getNodes(this)

        /// Returns the names of all the input nodes in the Process sequence to which no output points
        member this.FirstNodes = 
            ArcTables.getRootInputs(this)

        /// Returns the names of all the output nodes in the Process sequence that point to no input
        member this.LastNodes = 
            ArcTables.getFinalOutputs(this)

        /// Returns the names of all samples in the Process sequence
        member this.Samples =
            ArcTables.getNodesBy (fun (io : IOType) -> io.isSample) this

        /// Returns the names of all the input samples in the Process sequence to which no output points
        member this.FirstSamples = 
            ArcTables.getRootInputsBy (fun (io : IOType) -> io.isSample) this

        /// Returns the names of all the output samples in the Process sequence that point to no input
        member this.LastSamples = 
            ArcTables.getFinalOutputsBy (fun (io : IOType) -> io.isSample) this

        /// Returns the names of all sources in the Process sequence
        member this.Sources =
            ArcTables.getNodesBy (fun (io : IOType) -> io.isSource) this

        /// Returns the names of all data in the Process sequence
        member this.Data =
            ArcTables.getNodesBy (fun (io : IOType) -> io.isData) this

        /// Returns the names of all the input data in the Process sequence to which no output points
        member this.FirstData = 
            ArcTables.getRootInputsBy (fun (io : IOType) -> io.isData) this

        /// Returns the names of all the output data in the Process sequence that point to no input
        member this.LastData = 
            ArcTables.getFinalOutputsBy (fun (io : IOType) -> io.isData) this

    /// One Node of an ISA Process Sequence (Source, Sample, Data)
    type QNode(name : string, ioType : IOType, ?parentProcessSequence : ArcTables) =
    
        member this.DataContext = 
            if ioType.isData then 
                match DataContext.tryGetDataContextByName name with
                | Some dc -> 
                    DataContext(?id = dc.ID, name = name, ?format = dc.Format, ?selectorFormat = dc.SelectorFormat, ?explication = dc.Explication, ?unit = dc.Unit, ?objectType = dc.ObjectType, ?label = dc.Label, ?description = dc.Description, ?generatedBy = dc.GeneratedBy, comments = dc.Comments)
                    |> Some
                | None -> Some (DataContext(name = name))
            else 
                None

        member this.FilePath = 
            if ioType.isData then 
                this.DataContext.Value.FilePath.Value
            else 
                ""

        member this.Selector = 
            if ioType.isData then 
                this.DataContext.Value.Selector
            else 
                None

        /// Returns the process sequence in which the node appears
        member this.ParentProcessSequence = parentProcessSequence |> Option.defaultValue (ArcTables(ResizeArray []))

        /// Identifying name of the node
        member this.Name = name

        /// Type of node (source, sample, data, raw data ...)
        member this.IOType : IOType = ioType

        interface System.IEquatable<QNode> with
            member this.Equals other = other.Name.Equals this.Name

        override this.Equals other =
            match other with
            | :? QNode as p -> (this :> System.IEquatable<_>).Equals p
            | _ -> false

        override this.GetHashCode () = this.Name.GetHashCode()

        interface System.IComparable with
            member this.CompareTo other =
                match other with
                | :? QNode as p -> (this :> System.IComparable<_>).CompareTo p
                | _ -> -1

        interface System.IComparable<QNode> with
            member this.CompareTo other = other.Name.CompareTo this.Name

        /// Returns true, if the node is a source
        member this.isSource = this.IOType.isSource

        /// Returns true, if the node is a sample
        member this.isSample = this.IOType.isSample
    
        /// Returns true, if the node is a data
        member this.isData = this.IOType.isData

        /// Returns true, if the node is a material
        member this.isMaterial = this.IOType.isMaterial


    [<AutoOpen>]
    module QNodeExtensions =

        type QNode with

            /// Returns all other nodes in the process sequence, that are connected to this node
            member this.Nodes = this.ParentProcessSequence.NodesOf(this)

            /// Returns all other nodes in the process sequence, that are connected to this node and have no more origin nodes pointing to them
            member this.FirstNodes = this.ParentProcessSequence.FirstNodesOf(this)

            /// Returns all other nodes in the process sequence, that are connected to this node and have no more sink nodes they point to
            member this.LastNodes = this.ParentProcessSequence.LastNodesOf(this)

            /// Returns all other samples in the process sequence, that are connected to this node
            member this.Samples = this.ParentProcessSequence.SamplesOf(this)

            /// Returns all other samples in the process sequence, that are connected to this node and have no more origin nodes pointing to them
            member this.FirstSamples = this.ParentProcessSequence.FirstSamplesOf(this)
        
            /// Returns all other samples in the process sequence, that are connected to this node and have no more sink nodes they point to
            member this.LastSamples = this.ParentProcessSequence.LastSamplesOf(this)

            /// Returns all other sources in the process sequence, that are connected to this node
            member this.Sources = this.ParentProcessSequence.SourcesOf(this)

            /// Returns all other data in the process sequence, that are connected to this node
            member this.Data = this.ParentProcessSequence.FirstDataOf(this)

            /// Returns all other data in the process sequence, that are connected to this node and have no more origin nodes pointing to them
            member this.FirstData = this.ParentProcessSequence.FirstDataOf(this)

            /// Returns all other data in the process sequence, that are connected to this node and have no more sink nodes they point to
            member this.LastData = this.ParentProcessSequence.LastNodesOf(this)

            /// Returns all values in the process sequence, that are connected to this given node
            member this.Values = this.ParentProcessSequence.ValuesOf(this)

            /// Returns all values in the process sequence, that are connected to this given node and come before it in the sequence
            member this.PreviousValues = this.ParentProcessSequence.PreviousValuesOf(this)

            /// Returns all values in the process sequence, that are connected to the given node and come after it in the sequence
            member this.SucceedingValues = this.ParentProcessSequence.SucceedingValuesOf(this)

            /// Returns all characteristic values in the process sequence, that are connected to the given node
            member this.Characteristics = this.ParentProcessSequence.CharacteristicsOf(this)

            /// Returns all characteristic values in the process sequence, that are connected to the given node and come before it in the sequence
            member this.PreviousCharacteristics = this.ParentProcessSequence.PreviousCharacteristicsOf(this)

            /// Returns all characteristic values in the process sequence, that are connected to the given node and come after it in the sequence
            member this.SucceedingCharacteristics = this.ParentProcessSequence.SucceedingCharacteristicsOf(this)

            /// Returns all parameter values in the process sequence, that are connected to the given node
            member this.Parameters = this.ParentProcessSequence.ParametersOf(this)

            /// Returns all parameter values in the process sequence, that are connected to the given node and come before it in the sequence
            member this.PreviousParameters = this.ParentProcessSequence.PreviousParametersOf(this)

            /// Returns all parameter values in the process sequence, that are connected to the given node and come after it in the sequence
            member this.SucceedingParameters = this.ParentProcessSequence.SucceedingParametersOf(this)

           /// Returns all factor values in the process sequence, that are connected to the given node
            member this.Factors = this.ParentProcessSequence.FactorsOf(this)

            /// Returns all factor values in the process sequence, that are connected to the given node and come before it in the sequence
            member this.PreviousFactors = this.ParentProcessSequence.PreviousFactorsOf(this)

            /// Returns all factor values in the process sequence, that are connected to the given node and come after it in the sequence
            member this.SucceedingFactors = this.ParentProcessSequence.SucceedingFactorsOf(this)

            /// Returns all component values in the process sequence, that are connected to the given node
            member this.Components = this.ParentProcessSequence.ComponentsOf(this)

            /// Returns all component values in the process sequence, that are connected to the given node and come before it in the sequence
            member this.PreviousComponents = this.ParentProcessSequence.PreviousComponentsOf(this)

            /// Returns all component values in the process sequence, that are connected to the given node and come after it in the sequence
            member this.SucceedingComponents = this.ParentProcessSequence.SucceedingComponentsOf(this)
