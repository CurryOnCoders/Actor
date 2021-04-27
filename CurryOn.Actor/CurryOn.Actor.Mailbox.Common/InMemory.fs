namespace CurryOn.Actor.Mailbox

open CurryOn
open CurryOn.Actor
open System.Collections.Concurrent
open System.Threading.Tasks

module InMemory =
    let fifo<'message> () = 
        let queue = new ConcurrentQueue<IMessage<'message>>()
        let event = Event<IMessage<'message>>()
        let enqueued = event.Publish
        {new IMailbox<'message> with
            member __.Send message = 
                asyncResult {
                    queue.Enqueue(message)
                    let receipt = DeliveryReceipt.ofMessage message
                    event.Trigger(message)
                    return receipt :> IDeliveryReceipt
                }
            member __.Receive cancellation =
                let rec dequeue () =
                    asyncResult {
                        match queue.TryDequeue() with
                        | (true, message) ->
                            return Some message
                        | (false, _) ->
                            match cancellation with
                            | Some token when not token.IsCancellationRequested ->
                                return! dequeue ()
                            | _ ->
                                let tasks = 
                                    [
                                        Async.AwaitEvent enqueued |> Async.Ignore
                                        Async.Sleep 500
                                    ] |> List.map Async.StartAsTask

                                do! Task.WhenAny(tasks) |> Async.AwaitTask |> Async.Ignore
                                
                                if cancellation |> Option.map (fun c -> c.IsCancellationRequested) |> Option.defaultValue false then
                                    return None
                                else
                                    return! dequeue ()
                    }
                asyncResult {
                    let! message = dequeue()
                    match message with
                    | Some message ->
                        return message
                    | None ->
                        return! Error <| MailboxTimeout "Receive was cancelled before a message became available"
                }
        }