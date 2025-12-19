namespace ARCtrl.QueryModel

open ARCtrl
open System.IO

open System.Collections.Generic
open System.Collections

module CommentList = 
    let item (k : string) (comments : Comment list) =
        comments
        |> List.pick (fun c -> if c.Name.Value = k then Some c.Value else None)

    let tryItem (k : string) (comments : Comment list) =
        comments
        |> List.tryPick (fun c -> if c.Name.IsSome && c.Name.Value = k then Some c.Value else None)

type QCommentCollection(comments : Comment list) =

    new(comments : Comment list option) = QCommentCollection(Option.defaultValue [] comments) 

    member this.Comments = comments

    interface IEnumerable<Comment> with
        member this.GetEnumerator() = (Seq.ofList this.Comments).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() = (this :> IEnumerable<Comment>).GetEnumerator() :> IEnumerator

    member this.GetValueByName(key : string) =
        CommentList.item key this.Comments

    member this.TryGetValueByName(key : string) =
        CommentList.tryItem key this.Comments
