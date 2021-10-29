namespace CurryOn.Actor.Serialization

open CurryOn
open CurryOn.Actor
open MBrace.FsPickler.Json
open System.IO
open System.Text

module Serializer =
    let private serializer = JsonSerializer(indent = false, omitHeader = false)
    let private utf8 = UTF8Encoding(false)
    
    let toJson (x: 'a) =
        use stream = new MemoryStream()
        serializer.Serialize(stream, x)
        stream.ToArray() |> utf8.GetString
    
    let parseJson<'a> json =
        try
            use reader = new StringReader(json)
            Ok <| serializer.Deserialize<'a>(reader)
        with ex ->
            ex
            |> Exception.getMessage
            |> DeserializationError
            |> Error
