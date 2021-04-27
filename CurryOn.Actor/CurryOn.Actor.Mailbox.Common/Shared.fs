namespace CurryOn.Actor.Mailbox 

open CurryOn.Actor
open System

type DeliveryReceipt =
    {
        MessageId: Guid
        Timestamp: DateTimeOffset
    } interface IDeliveryReceipt with
        member this.MessageId = this.MessageId
        member this.Timestamp = this.Timestamp

module DeliveryReceipt =
    let ofMessage (message: IMessage<_>) =
        {
            MessageId = message.Id
            Timestamp = DateTimeOffset.UtcNow
        }