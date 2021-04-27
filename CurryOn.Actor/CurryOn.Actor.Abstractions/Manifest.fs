namespace CurryOn.Actor

type IManifestActor<'message, 'error> =
    inherit IActor<'message, 'error>
    inherit IActorBehavior<'message, 'error>
        
