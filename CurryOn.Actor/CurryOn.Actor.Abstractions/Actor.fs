namespace CurryOn.Actor

open CurryOn
open System
open System.Threading

type ActorError<'e> =
| MailboxError of MailboxError
| ActorLogicError of 'e
| UnknownActor of ActorLocation
| OperationCanceled of string
| UnexpectedActorError of exn

type IActor =
    abstract member Tell<'message, 'error> : 'message -> AsyncResult<IDeliveryReceipt, ActorError<'error>>
    abstract member Ask<'message, 'result, 'error> : 'message -> AsyncResult<'result, ActorError<'error>>    
    abstract member AskWithTimeout<'message, 'result, 'error> : TimeSpan -> 'message -> AsyncResult<'result, ActorError<'error>>
    abstract member AskWithCancellation<'message, 'result, 'error> : CancellationToken -> 'message -> AsyncResult<'result, ActorError<'error>>

type IActor<'message, 'error> = 
    abstract member Tell : 'message -> AsyncResult<IDeliveryReceipt, ActorError<'error>>
    abstract member Ask<'result> : 'message -> AsyncResult<'result, ActorError<'error>>
    abstract member AskWithTimeout<'result> : TimeSpan -> 'message -> AsyncResult<'result, ActorError<'error>>
    abstract member AskWithCancellation<'result> : CancellationToken -> 'message -> AsyncResult<'result, ActorError<'error>>


