namespace CurryOn.Actor.Mailbox

open CurryOn
open CurryOn.Actor
open CurryOn.Tasks
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
                taskResult {
                    queue.Enqueue(message)
                    let receipt = DeliveryReceipt.ofMessage message
                    event.Trigger(message)
                    return receipt :> IDeliveryReceipt
                }
            member __.Receive () =
                taskResult {
                    return
                        match queue.TryDequeue() with
                        | (true, message) ->
                            Some message
                        | (false, _) ->
                            None
                }
            member __.Subscribe f =
                let rec processAllInQueue () =
                    async {
                        match queue.TryDequeue() with
                        | (true, message) ->
                            do! f message |> Async.AwaitTask
                            return! processAllInQueue ()
                        | (false, _) ->
                            return ()
                    }
                task {
                    do! processAllInQueue () |> Async.StartAsTask              
                    let observer = enqueued |> Observable.subscribe (ignore >> processAllInQueue >> Async.RunSynchronously)
                    return observer
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
                taskResult {
                    let priority = getPriority message.Body
                    queues.AddOrUpdate(priority, (fun _ -> newQueue message), (fun _ queue -> queue |> updateQueue message)) |> ignore
                    let receipt = DeliveryReceipt.ofMessage message
                    event.Trigger(message)
                    return receipt :> IDeliveryReceipt
                }
            member __.Receive () =
                taskResult {
                    return getNextQueuedMessage ()
                }
            member __.Subscribe f =
                let rec processAllInQueue () =
                    async {
                        match getNextQueuedMessage() with
                        | Some message ->
                            do! f message |> Async.AwaitTask
                            return! processAllInQueue ()
                        | None ->
                            return ()
                    }

                task {
                    do! processAllInQueue () |> Async.StartAsTask                
                    let observer = enqueued |> Observable.subscribe (ignore >> processAllInQueue >> Async.RunSynchronously)
                    return observer
                }
        }