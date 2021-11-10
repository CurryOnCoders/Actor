namespace CurryOn.Actor

open CurryOn.Tasks
open FSharp.Quotations

[<Struct>] type Stateless = Stateless

type IActorState<'state> =
    abstract member IsStateless: bool
    abstract member Current: 'state
    abstract member Initial: 'state

type IActorBehavior<'state, 'message, 'result, 'error> =
    abstract member ProcessMessage : Expr<'message -> TaskResult<'result * 'state, 'error>>

type IUnitOfWork<'state, 'message, 'result, 'error> =    
    abstract member Message: 'message
    abstract member State: IActorState<'state>
    abstract member Behavior: IActorBehavior<'state, 'message, 'result, 'error>

type IEvaluator =
    abstract member Evaluate<'state, 'message, 'result, 'error> : IUnitOfWork<'state, 'message, 'result, 'error> -> TaskResult<'result * 'state, 'error>

type IDispatcher =
    abstract member Evaluator: IEvaluator
    abstract member Dispatch<'state, 'message, 'result, 'error> : 'state -> 'message -> TaskResult<'result * 'state, 'error>



