namespace CurryOn.Actor.Mailbox

open CurryOn
open CurryOn.Actor
open System.Collections.Concurrent
open System.Threading.Tasks

module InMemory =
    /// Use an in-memory FIFO queue to process messages in-order
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

    /// Use a priority queue to order messages from low to high based on a priority function
    let priority<'message> (getPriority: 'message -> int) = 
        let queues = new ConcurrentDictionary<int, ConcurrentQueue<IMessage<'message>>>()
        
        let updateQueue message (queue: ConcurrentQueue<IMessage<'message>>)  =
            queue.Enqueue(message)
            queue

        let newQueue message = 
            new ConcurrentQueue<IMessage<'message>>()
            |> updateQueue message

        let getNextQueuedMessage () =
            seq {            
                for priorityLevel in queues.Keys |> Seq.sort do
                    match queues.TryGetValue(priorityLevel) with
                    | (true, queue) ->
                        match queue.TryDequeue() with
                        | (true, message) ->
                            yield message
                        | _ ->
                            ()
                    | _ ->
                        ()
            } |> Seq.tryHead
            
        let event = Event<IMessage<'message>>()
        let enqueued = event.Publish

        {new IMailbox<'message> with
            member __.Send message = 
                asyncResult {
                    let priority = getPriority message.Body
                    queues.AddOrUpdate(priority, (fun _ -> newQueue message), (fun _ queue -> queue |> updateQueue message)) |> ignore
                    let receipt = DeliveryReceipt.ofMessage message
                    event.Trigger(message)
                    return receipt :> IDeliveryReceipt
                }
            member __.Receive cancellation =
                let rec dequeue () =
                    asyncResult {
                        match getNextQueuedMessage() with
                        | Some message ->
                            return Some message
                        | None ->
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