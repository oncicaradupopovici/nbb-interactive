#r "nuget: NBB.Messaging.Host, 6.0.18"
#r "nuget: NBB.Messaging.Nats, 6.0.18"
#r "nuget: Microsoft.Extensions.DependencyInjection, 6.0.0"
#r "nuget: Microsoft.Extensions.Hosting, 6.0.1"
#r "nuget: Microsoft.Extensions.Configuration, 6.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 6.0.0"
#r "nuget: Microsoft.Extensions.Logging, 6.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 6.0.0"
#r "nuget: Moq, 4.16.1"

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open NBB.Messaging.Host
open Moq
open System.Threading.Tasks
open System.IO
open NBB.Messaging.Abstractions
open NBB.Core.Pipeline

type MyCommand = { id: int }

let buildContainer () =
    let configuration =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .Build()

    let services = ServiceCollection()

    services.AddSingleton<IHostApplicationLifetime>(Mock.Of<IHostApplicationLifetime>())
    |> ignore

    services.AddSingleton<IConfiguration>(configuration)
    |> ignore

    services.AddLogging (fun x ->
        x.AddConsole().SetMinimumLevel(LogLevel.Debug)
        |> ignore)
    |> ignore

    services
        .AddMessageBus()
        .AddNatsTransport(configuration)
    |> ignore

    services.AddMessagingHost(
        configuration,
        fun h ->
            h.Configure (fun configBuilder ->
                configBuilder
                    .AddSubscriberServices(fun c -> c.FromTopic("MyCommand") |> ignore)
                    .WithDefaultOptions()
                    .UsePipeline(fun pipelineBuilder ->
                        pipelineBuilder.UseExceptionHandlingMiddleware()
                        // .Use(fun next ->
                        //     (PipelineDelegate<MessagingContext> (fun ctx ct ->
                        //         let logger = ctx.Services.GetRequiredService<ILogger<MessageBus>>()
                        //         logger.LogInformation("Helooooooooooooooooooooooooo")
                        //         next.Invoke(ctx, ct))))
                        |> ignore)
                |> ignore)
            |> ignore
    )
    |> ignore

    services.BuildServiceProvider()


let runMessagingHost (container: ServiceProvider) =
    task {
        let msgHost = container.GetRequiredService<IMessagingHost>()
        do! msgHost.StartAsync()
        // do! Task.Delay(100000)
        // do! msgHost.StopAsync()
        return msgHost
    }


let publish (container: ServiceProvider) (cnt: int) =
    let msgBus = container.GetRequiredService<IMessageBusPublisher>()
    let o = new MessagingPublisherOptions()
    o.TopicName <- "MyCommand"

    let t =
        task {
            for i in 1..cnt do
                do! msgBus.PublishAsync({ id = i }, o)
        }

    t.Wait()

let testLog () =
    let c = buildContainer ()
    let logger = c.GetRequiredService<ILogger<MessageBus>>()
    logger.LogDebug("debug________________")
    logger.LogInformation(Directory.GetCurrentDirectory())
