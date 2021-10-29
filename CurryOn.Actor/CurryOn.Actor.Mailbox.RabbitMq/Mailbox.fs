namespace CurryOn.Actor.Mailbox.RabbitMq

open CurryOn
open CurryOn.Actor
open CurryOn.Actor.Serialization
open CurryOn.Tasks
open RabbitMQ.Client
open System

type internal RabbitMqChannel =
    {
        Connection: IConnection
        Channel: IModel
        Properties: IBasicProperties
    } interface IDisposable with
        member this.Dispose () =
            this.Channel.Dispose()
            this.Connection.Dispose()


[<AbstractClass>]
type RabbitMqMailboxBase (configuration: IRabbitMqMailboxConfiguration) =
    let primaryHost = configuration.Hosts |> Array.tryHead |> Option.defaultValue "localhost"
    let connectionFactory = ConnectionFactory(HostName = primaryHost, Port = configuration.Port, VirtualHost = configuration.VirtualHost, UserName = configuration.UserName, Password = configuration.Password)
    
    let getConnection () =
        connectionFactory.CreateConnection(configuration.Hosts)

    let openChannel () =
        let connection = getConnection()
        let channel = connection.CreateModel()
        let prefetchCount =  configuration.PrefetchCount |> Option.map uint16 |> Option.defaultValue 100us
        channel.BasicQos(0ul, prefetchCount, false)
        { Connection = connection; Channel = channel; Properties = channel.CreateBasicProperties() }

    let connection = lazy(openChannel ())

    member internal this.Connect () = connection.Value

type RabbitMqOutgoingMailbox (configuration, actorName) =
    inherit RabbitMqMailboxBase(configuration)

    member this.Send (message: IMessage<_>) =
        let rabbitMq = this.Connect()
        taskResult {
            let json = message |> Serializer.toJson |> Utf8.toBytes
            let body = ReadOnlyMemory<byte>(json)
            rabbitMq.Channel.BasicPublish(configuration.RootExchange, actorName, rabbitMq.Properties, body)
        }
        