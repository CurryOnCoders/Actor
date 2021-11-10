namespace CurryOn.Actor.Mailbox.RabbitMq

open CurryOn
open CurryOn.Actor
open CurryOn.Actor.Mailbox
open CurryOn.Actor.Serialization
open CurryOn.RabbitMq
open CurryOn.Tasks
open RabbitMQ.Client
open System
open System.Threading
open System.Threading.Tasks

[<AbstractClass>]
type RabbitMqMailboxBase (configuration: IRabbitMqMailboxConfiguration, actorName: string, persistent: bool) =
    let channelManager = new RabbitMqChannelManager(configuration)    
    let exchange = configuration.RootExchange
    let queue = sprintf "%s.%s" configuration.RootExchange actorName
    
    do 
        async {
            let! channel = channelManager.GetChannel() |> Async.AwaitTask
            channel.ExchangeDeclare(exchange, "topic", true, false, null)
            channel.QueueDeclare(queue, true, false, not persistent, null) |> ignore
            channel.QueueBind(queue, exchange, actorName, null)
        } |> Async.RunSynchronously

    member __.ChannelManager = channelManager
    member __.Topic = exchange
    member __.QueueName = queue

type RabbitMqOutboundMailbox<'message> (configuration: IRabbitMqMailboxConfiguration, actorName: string, persistent: bool) =
    inherit RabbitMqMailboxBase(configuration, actorName, persistent)

    member this.Send (message: IMessage<'message>) =
        taskResult {
            let! context = this.ChannelManager.GetContext()
            let json = message |> Serializer.toJson |> Utf8.toBytes
            let body = ReadOnlyMemory<byte>(json)
            try
                do context.Channel.BasicPublish(configuration.RootExchange, actorName, true, context.Properties, body)
                return { MessageId = message.Id; Timestamp = DateTimeOffset.UtcNow } :> IDeliveryReceipt
            with ex ->
                return! Error <| CommunicationError ex.Message
        }

    interface IOutboundMailbox<'message> with
        member this.Send message = this.Send message

type RabbitMqMailboxConsumer<'a> (channel, actorName, f: IMessage<'a> -> Task) =
    inherit AsyncDefaultBasicConsumer(channel)

    override this.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =
        task {
            if routingKey = actorName then
                let json = body.ToArray() |> Utf8.toString
                let result = json |> Serializer.parseJson<MailboxMessage<'a>>
                match result with
                | Ok message ->     
                    try
                        do! f message
                        channel.BasicAck(deliveryTag, false)
                    with ex ->
                        // TODO: Report error
                        channel.BasicNack(deliveryTag, false, false)
                | Error error ->
                    return raise <| ActorSystemException error
            else
                channel.BasicReject(deliveryTag, false)
        } :> Task


        
type RabbitMqInboundMailbox<'message> (configuration: IRabbitMqMailboxConfiguration, actorName: string, persistent: bool, competing: bool) =
    inherit RabbitMqMailboxBase(configuration, actorName, persistent)

    member this.Receive () : TaskResult<IMessage<'message> option, MailboxError> =
        taskResult {
            let! (channel: IModel) = this.ChannelManager.GetChannel()
            let getResult = channel.BasicGet(this.QueueName, false)

            if getResult.RoutingKey = actorName then
                let json = getResult.Body.ToArray() |> Utf8.toString
                let result = json |> Serializer.parseJson<MailboxMessage<'message>>
                match result with
                | Ok message ->     
                    channel.BasicAck(getResult.DeliveryTag, false)
                    return Some (message :> IMessage<'message>)
                | Error error ->
                    channel.BasicNack(getResult.DeliveryTag, false, false)
                    return! Error (SystemError error)
            else
                channel.BasicReject(getResult.DeliveryTag, false)
                return None

        }
        
    member this.Subscribe (f: IMessage<'message> -> Task) =
        task {
            let! channel = this.ChannelManager.GetNewChannel()
            let consumer = new RabbitMqMailboxConsumer<'message>(channel, actorName, f)
            let tag = channel.BasicConsume(this.QueueName, false, consumer)
            return 
                { new IDisposable with
                    member __.Dispose () =
                        channel.Dispose()
                }
        }
        
    interface IInboundMailbox<'message> with
        member this.Receive () = this.Receive()
        member this.Subscribe f = this.Subscribe f