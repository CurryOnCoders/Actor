namespace CurryOn.Actor.Mailbox.RabbitMq

open CurryOn.RabbitMq
open System

type IRabbitMqMailboxConfiguration =
    inherit IRabbitMqConfiguration
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

