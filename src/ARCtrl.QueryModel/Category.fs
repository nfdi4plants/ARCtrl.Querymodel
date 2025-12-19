namespace ARCtrl.QueryModel

open ARCtrl

[<AutoOpen>]
module CategoryExtensions = 

    type CompositeHeader with

        member this.IsCharacteristicCategory =
            match this with
            | CompositeHeader.Characteristic _  -> true
            | _                 -> false

        member this.IsParameterCategory =
            match this with
            | CompositeHeader.Parameter _   -> true
            | _             -> false

        member this.IsFactorCategory =
            match this with
            | CompositeHeader.Factor _  -> true
            | _         -> false

        member this.IsComponentType =
            match this with
            | CompositeHeader.Component _  -> true
            | _         -> false


        /// Returns the category of the Category
        member this.Category = 
            match this with
            | CompositeHeader.Parameter p       -> p
            | CompositeHeader.Characteristic c  -> c
            | CompositeHeader.Factor f          -> f
            | CompositeHeader.Component c       -> c

        /// Returns the name of the Category as string
        member this.NameText = this.Category.NameText

        /// Returns the header text of the Category as string
        member this.HeaderText = 
            this.ToString()