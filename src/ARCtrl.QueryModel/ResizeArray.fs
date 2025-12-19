namespace ARCtrl.QueryModel


/// Standard Collection operations for resize array
module ResizeArray =

    let map  f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            b.Add(f i)
        b       

    let choose f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            match f i with
            | Some x -> b.Add(x)
            | None -> ()
        b

    let filter f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            if f i then b.Add(i)
        b

    let fold f s (a : ResizeArray<_>) =
        let mutable state = s
        for i in a do
            state <- f state i
        state

    let foldBack f (a : ResizeArray<_>) s =
        let mutable state = s
        for i in a do
            state <- f i state
        state

    let iter f (a : ResizeArray<_>) =
        for i in a do
            f i

    let reduce f (a : ResizeArray<_>) =
        match a with
        | a when a.Count = 0 -> failwith "ResizeArray.reduce: empty array"
        | a when a.Count = 1 -> a.[0]
        | a -> 
            let mutable state = a.[0]
            for i in 1 .. a.Count - 1 do
                state <- f state a.[i]
            state

    let collect f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            let c = f i
            for j in c do
                b.Add(j)
        b

    let distinct (a : ResizeArray<_>) =
        //let s = System.Collections.Generic.HashSet<_>()
        //let b = ResizeArray<_>()
        //for i in a do
        //    if s.Add(i) then          
        //        b.Add(i)
        //b
        Seq.distinct a
        |> ResizeArray


    let distinctBy f (a : ResizeArray<_>) =
        //let s = System.Collections.Generic.HashSet<_>()
        //let b = ResizeArray<_>()
        //for i in a do
        //    let k = f i
        //    if s.Add(k) then
        //        b.Add(k)
        //b
        Seq.distinctBy f a
        |> ResizeArray