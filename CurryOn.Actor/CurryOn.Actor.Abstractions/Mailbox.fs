namespace CurryOn.Actor

open CurryOn.Tasks
open System
open System.Threading
open System.Threading.Tasks

type MailboxError =
| CommunicationError of string
| MailboxFullError
| MailboxTimeout of string
| SystemError of ActorSystemError
| UnexpectedMailboxError of exn

type IMessage<'message> =
    abstract member Id: Guid
    abstract member Sender: ActorLocation
    abstract member ReplyTo: ActorLocation
    abstract member Timestamp: DateTimeOffset
    abstract member Body: 'message

type IDeliveryReceipt =
    abstract member MessageId: Guid
    abstract member Timestamp: DateTimeOffset

type IOutboundMailbox<'message> =
    abstract member Send : IMessage<'message> -> TaskResult<IDeliveryReceipt, MailboxError>

type IInboundMailbox<'message> =
    abstract member Receive : unit -> TaskResult<IMessage<'message> option, MailboxError>
    abstract member Subscribe : (IMessage<'message> -> Task) -> Task<IDisposable>

type IMailbox<'message> =
    inherit IOutboundMailbox<'message>
    inherit IInboundMailbox<'message>

module Mailbox =
    let asInboundOnly<'message> mailbox =
        mailbox :> IInboundMailbox<'message>

    let asOutboundOnly<'message> mailbox =
        mailbox :> IOutboundMailbox<'message>

    let fromInboundOutbound<'message> (inboundMailbox: IInboundMailbox<'message>) (outboundMailbox: IOutboundMailbox<'message>) =
        {new IMailbox<'message> with
            member __.Send message = outboundMailbox.Send message
            member __.Receive () = inboundMailbox.Receive ()
            member __.Subscribe f = inboundMailbox.Subscribe f
        }

type MailboxMessage<'message> =
    {
        Id: Guid
        Sender: ActorLocation
        ReplyTo: ActorLocation
        Timestamp: DateTimeOffset
        Body: 'message
    } interface IMessage<'message> with
        member this.Id = this.Id
        member this.Sender = this.Sender
        member this.ReplyTo = this.ReplyTo
        member this.Timestamp = this.Timestamp
        member this.Body = this.Body

module Message =
    let create (replyTo: ActorLocation option) (sender: ActorLocation option) (body: 'message) =
        let sendFrom = sender |> Option.defaultValue CurrentActor
        {
            Id = Guid.NewGuid()
            Sender = sendFrom
            ReplyTo = replyTo |> Option.defaultValue sendFrom
            Timestamp = DateTimeOffset.UtcNow
            Body = body
        } :> IMessage<'message>

    let ofBody (body: 'message) =
        body |> create None None

    let ofAny<'a> (message: 'a) =
        match box message with
        | :? IMessage<'a> as actorMessage -> actorMessage
        | _ -> ofBody message