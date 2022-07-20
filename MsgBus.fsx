#r "nuget: NBB.Messaging.Nats, 6.0.19"
#r "nuget: Microsoft.Extensions.DependencyInjection, 6.0.0"
#r "nuget: Microsoft.Extensions.Hosting, 6.0.1"
#r "nuget: Microsoft.Extensions.Configuration, 6.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 6.0.0"
#r "nuget: Microsoft.Extensions.Logging, 6.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 6.0.0"

#load "CompositionRoot.fsx"

open Microsoft.Extensions.DependencyInjection
open NBB.Messaging.Abstractions
open System.Threading.Tasks

let c = CompositionRoot.buildContainer (fun _ _ -> ())

let pub topic cnt =
    let msgBus = c.GetRequiredService<IMessageBusPublisher>()
    let o = MessagingPublisherOptions.Default
    o.TopicName <- topic

    let t =
        task {
            for i in 1..cnt do
                do! msgBus.PublishAsync({|id=i|}, o)
        }

    t.Wait()

let sub topic =
    let msgBus = c.GetRequiredService<IMessageBusSubscriber>()
    let h _envelope = Task.CompletedTask
    let o = MessagingSubscriberOptions.Default
    o.TopicName <- topic
    msgBus.SubscribeAsync(h, o).Result