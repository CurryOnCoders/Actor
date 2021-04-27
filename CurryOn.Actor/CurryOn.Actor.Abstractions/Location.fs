namespace CurryOn.Actor

open CurryOn
open System
open System.Net

[<Struct>]
type Address = private Address of Uri

[<Struct>]
type ActorName = private ActorName of string

type ActorHost = 
| HostName of string
| IpAddress of IPAddress

[<Struct>]
type ActorHostAlias = private ActorHostAlias of string

[<Struct>]
type ActorSystemName = private ActorSystemName of string

type ActorLocation =
| LocateByName of ActorName
| LocateByAddress of Address

type ActorLocationError =
| InvalidAddress of string
| InvalidName of string
| UnsupportedProtocol of string

type ActorNetworkingError =
| InvalidHostName of string
| InvalidIpAddress of string
| InvalidHostAlias of string
| UnresolvableAddress of string
| PortAlreadyInIuse of int

type ActorSystemPort =
| DynamicPort
| DefaultPort
| StaticPort of int

type INetworkLocation =
    abstract member Host: ActorHost
    abstract member Alias: ActorHostAlias option
    abstract member Port: ActorSystemPort

type ActorSystemNetworking =
| DisableNetworking
| EnableNeworking of INetworkLocation

module ActorLocation =
    let internal validateName name =
        name |> String.exists (fun c -> not (Char.IsLetterOrDigit c || c = '-'))

    let addressOf (ActorSystemName system) (ActorName name) =
        sprintf "%s%s/%s" ActorScheme system name 
        |> Uri
        |> Address

    let locationOf system name =
        name |> addressOf system |> LocateByAddress

module RemoteActorLocation =
    let addressOf host port (ActorSystemName system) (ActorName name) =
        match port with
        | Some port ->
            sprintf "%s%s@%s:%d/%s" ActorScheme system host port name
        | None ->
            sprintf "%s%s@%s/%s" ActorScheme system host name
        |> Uri
        |> Address

    let locationOf host port system name =
        name |> addressOf host port system |> LocateByAddress

module Address =
    let create (url: string) =
        try
            let uri = Uri url
            if uri.Scheme.Equals(ActorScheme, StringComparison.InvariantCultureIgnoreCase)
            then Ok <| Address uri
            else Error <| UnsupportedProtocol (sprintf "Protocol '%s' is not support, Actor Locations must use the actor:// scheme" uri.Scheme)
        with ex ->
            Error <| InvalidAddress ex.Message

    let url (Address url) = url

    let toString (Address url) = url.ToString()

    let location address = LocateByAddress address

module ActorName =
    let create (name: string) =
        if name |> ActorLocation.validateName
        then Error <| InvalidName "The actor name '%s' is not supported, only letters, digits, and '-' (dashes) may be used in actor names"
        else Ok <| ActorName name

    let toString (ActorName name) = name

    let location name = LocateByName name

module ActorHost =
    let ofHostName (name: string) =
        if name |> String.IsNullOrWhiteSpace |> not
        then Error <| InvalidHostName "The host name must not be blank"
        else Ok <| HostName name

    let ofIpAddress (ipAddress: string) =
        try
            let ip = IPAddress.Parse(ipAddress)
            Ok <| IpAddress ip
        with ex ->
            Error <| InvalidIpAddress ex.Message

    let toString = function
    | HostName name -> name
    | IpAddress ip -> ip.ToString()

    let toHostName = function
    | HostName name -> 
        Ok name
    | IpAddress ip -> 
        try 
            let dnsEntry = Dns.GetHostEntry(ip)
            Ok dnsEntry.HostName
        with ex -> 
            Error <| UnresolvableAddress ex.Message

    let toIpAddress = function
    | HostName name ->
        try
            let ips = Dns.GetHostAddresses(name)
            match ips |> Seq.tryFind (fun ip -> ip.AddressFamily = Sockets.AddressFamily.InterNetwork) with
            | Some ip -> 
                Ok ip
            | None -> 
                ips 
                |> Seq.tryHead 
                |> Result.ofOption (UnresolvableAddress <| sprintf "No IP Address found for host %s" name)
        with ex -> 
            Error <| UnresolvableAddress ex.Message
    | IpAddress ip ->
        Ok ip


module ActorHostAlias =
    let create (alias: string) =
        if alias |> String.IsNullOrWhiteSpace |> not
        then Error <| InvalidHostName "The host alias must not be blank"
        else Ok <| ActorHostAlias alias

    let toString (ActorHostAlias alias) = alias

module ActorSystemName =
    let create (name: string) =
        if name |> ActorLocation.validateName
        then Error <| InvalidSystemName "The actor system name '%s' is not supported, only letters, digits, and '-' (dashes) may be used in actor system names"
        else Ok <| ActorSystemName name

    let toString (ActorSystemName name) = name