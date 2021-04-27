namespace CurryOn.Actor

open CurryOn

type IActorSystemHost =
    abstract member HostName: string
    abstract member Port: int option

type IActorBehavior<'message, 'error> =
    abstract member ProcessMessage<'result> : 'message -> AsyncResult<'result, 'error>

type IActorSystemMetadata =    
    abstract member Name: ActorSystemName
    abstract member Host: IActorSystemHost

type IActorFactory =
    abstract member Spawn<'message, 'error> : IActorBehavior<'message, 'error> -> IActor<'message, 'error>

type IActorSystemContext =
    inherit IActorFactory
    abstract member System: IActorSystemMetadata   
    abstract member AddressOf: ActorName -> Address
    abstract member RemoteAddressOf: ActorName -> Address
    abstract member LocationOf: ActorName -> ActorLocation

type ExitCode =
| Default = 0
| HostProcessTerminating = 1
| UserRequestedShutdown = 2
| InvalidStateDetected = 3
| SupervisorMandatedShutdown = 4
| IntentionallyUnspecified = 5
| CustomShutdownReason = 99

type IActorSystemShutdownReason =
    abstract member Message: string
    abstract member ExitCode: ExitCode

type ShutdownResult =
| ShutdownSuccessfully of IActorSystemShutdownReason
| ShutdownFailed of ActorSystemError

module ShutdownReason =
    let Default =
        { new IActorSystemShutdownReason with
            member __.Message = "Actor System is Shutting Down"
            member __.ExitCode = ExitCode.Default
        }

type IActorSystem =
    inherit IActorSystemMetadata
    abstract member Factory: IActorFactory
    abstract member Context: IActorSystemContext
    abstract member Network: ActorSystemNetworking
    abstract member Shutdown: IActorSystemShutdownReason -> Async<ShutdownResult>