namespace CurryOn.Actor

type NoError = NoError

type ActorSystemError =
    | AlreadyInUse of string
    | InvalidSystemName of string
    | ConfigurationError of string
    | DeserializationError of string
    | UnexpectedSystemError of exn
    member error.Message =
        match error with
        | AlreadyInUse id -> sprintf "Address Already in Use: %s" id
        | InvalidSystemName name -> sprintf "The System Name was Invalid: %s" name
        | ConfigurationError e -> sprintf "Configuration Error: %s" e
        | DeserializationError e -> sprintf "Error Deserializing Message: %s" e
        | UnexpectedSystemError ex -> sprintf "Unexpected Exception: %s" ex.Message

type ActorSystemException (error: ActorSystemError) =
    inherit System.Exception(error.Message)
    member __.Error = error

[<AutoOpen>]
module Constants =
    [<Literal>] 
    let ActorScheme = "actor://"

    [<Literal>] 
    let DefaultPort = 38838