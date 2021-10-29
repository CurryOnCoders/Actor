namespace CurryOn.Actor.Mailbox.RabbitMq

open System

type IRabbitMqMailboxConfiguration =
    abstract member Hosts: string []
    abstract member Port: int
    abstract member UserName: string
    abstract member Password: string
    abstract member PrefetchCount: int option
    abstract member VirtualHost: string
    abstract member RootExchange: string

[<CLIMutable>]
type RabbitMqMailboxConfiguration =
    {
        Hosts: string []
        Port: Nullable<int>
        UserName: string
        Password: string
        PrefetchCount: Nullable<int>
        VirtualHost: string
        RootExchange: string
    } interface IRabbitMqMailboxConfiguration with
        member this.Hosts = this.Hosts
        member this.Port = this.Port |> Option.ofNullable |> Option.defaultValue 5672
        member this.UserName = this.UserName
        member this.Password = this.Password
        member this.PrefetchCount = this.PrefetchCount |> Option.ofNullable
        member this.VirtualHost = this.VirtualHost |> Option.ofObj |> Option.defaultValue "/"
        member this.RootExchange = this.RootExchange |> Option.ofObj |> Option.defaultValue "CurryOn.Actor.Mailbox"

