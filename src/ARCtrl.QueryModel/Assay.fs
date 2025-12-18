namespace ARCtrl.QueryModel

open ARCtrl
open System.IO

open System.Collections.Generic

[<AutoOpen>]
module ArcAssayExtensions = 

    /// Queryable representation of an ISA Assay. Implements the ArcTables interface
    type ArcAssay with 

        /// get the protocol or sheet (in ISATab logic) with the given name
        member this.Protocol (sheetName : string) =
            this.GetTable sheetName

        /// get the nth protocol or sheet (in ISATab logic) 
        member this.Protocol (index : int) =
            this.GetTableAt index

        /// Returns the initial inputs final outputs of the assay, to which no processPoints
        static member getRootInputs (assay : ArcAssay) = ArcTables.getRootInputs assay

        /// Returns the final outputs of the assay, which point to no further nodes
        static member getFinalOutputs (assay : ArcAssay) = ArcTables.getFinalOutputs assay

        /// Returns the initial inputs final outputs of the assay, to which no processPoints
        static member getRootInputOf (assay : ArcAssay) (sample : string) = ArcTables.getRootInputsOfBy (fun _ -> true) sample assay 
        
        /// Returns the final outputs of the assay, which point to no further nodes
        static member getFinalOutputsOf (assay : ArcAssay) (sample : string) = ArcTables.getFinalOutputsOfBy (fun _ -> true) sample assay
