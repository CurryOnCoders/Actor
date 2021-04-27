namespace CurryOn.Actor

open CurryOn
open FSharp.Quotations

[<Struct>] type Stateless = Stateless

type IActorState<'state> =
    abstract member IsStateless: bool
    abstract member Current: 'state
    abstract member Initial: 'state

type IActorBehavior<'message, 'result, 'error> =
    abstract member ProcessMessage : Expr<'message -> AsyncResult<'result, 'error>>

type IUnitOfWork<'state, 'message, 'result, 'error> =    
    abstract member Message: 'message
    abstract member State: IActorState<'state>
    abstract member Behavior: IActorBehavior<'message, 'result, 'error>

type IEvaluator =
    abstract member Evaluate<'state, 'message, 'result, 'error> : IUnitOfWork<'state, 'message, 'result, 'error> -> AsyncResult<'result, 'error>

type IDispatcher =
    abstract member Evaluator: IEvaluator
    abstract member Dispatch<'state, 'message, 'result, 'error> : 'state -> 'message -> AsyncResult<'result, 'error>



