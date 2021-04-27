namespace CurryOn.Actor

open CurryOn
open System
open System.Threading

type MailboxError =
| CoummunicationError of string
| MailboxFullError
| MailboxTimeout of string
| UnexpectedMailboxError of exn

type IDeliveryReceipt =
    abstract member MessageId: string
    abstract member Timestamp: DateTimeOffset

type IOutboundMailbox<'message> =
    abstract member Send : 'message -> AsyncResult<IDeliveryReceipt, MailboxError>

type IInboundMailbox<'message> =
    abstract member Receive : CancellationToken option -> AsyncResult<'message, MailboxError>

type IMailbox<'message> =
    inherit IOutboundMailbox<'message>
    inherit IInboundMailbox<'message>