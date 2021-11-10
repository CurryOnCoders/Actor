namespace CurryOn.Actor

open CurryOn.Tasks
open System
open System.Threading

type ActorError<'e> =
| MailboxError of MailboxError
| ActorLogicError of 'e
| UnknownActor of ActorLocation
| OperationCanceled of string
| UnexpectedActorError of exn

type IActor =
    abstract member Tell<'message, 'error> : 'message -> TaskResult<IDeliveryReceipt, ActorError<'error>>
    abstract member Ask<'message, 'result, 'error> : 'message -> TaskResult<'result, ActorError<'error>>    
    abstract member AskWithTimeout<'message, 'result, 'error> : TimeSpan -> 'message -> TaskResult<'result, ActorError<'error>>
    abstract member AskWithCancellation<'message, 'result, 'error> : CancellationToken -> 'message -> TaskResult<'result, ActorError<'error>>

type IActor<'message, 'error> = 
    abstract member Tell : 'message -> TaskResult<IDeliveryReceipt, ActorError<'error>>
    abstract member Ask<'result> : 'message -> TaskResult<'result, ActorError<'error>>
    abstract member AskWithTimeout<'result> : TimeSpan -> 'message -> TaskResult<'result, ActorError<'error>>
    abstract member AskWithCancellation<'result> : CancellationToken -> 'message -> TaskResult<'result, ActorError<'error>>


