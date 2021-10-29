namespace CurryOn.Actor

type NoError = NoError

type ActorSystemError =
| AlreadyInUse of string
| InvalidSystemName of string
| ConfigurationError of string
| DeserializationError of string
| UnexpectedSystemError of exn

[<AutoOpen>]
module Constants =
    [<Literal>] 
    let ActorScheme = "actor://"

    [<Literal>] 
    let DefaultPort = 38838