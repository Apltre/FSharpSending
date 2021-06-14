namespace FSharpSending.Common.Helpers.RabbitMQ

open RabbitMQ.Client
open Logger
open FSharpSending.Common.Types.CommonTypes
open RabbitMQ.Client.Events
open System
open System.Text
open System.Collections.Generic

module RabbitQueueConsumer =
    let consumedHandler (handler: string -> Async<Result<unit, Errors>>) (LogErrorFunc logError) (data:ReadOnlyMemory<byte>) = async {
               let body = data.ToArray()
               let message = Encoding.UTF8.GetString body
               let! result = handler message
               result
               |> Result.teeError logError
               |> ignore
    }

    let log (LogErrorFunc logError) message =
        logError message

    let handleLostMessages (LogErrorFunc log) (dictionary: Dictionary<uint64, DateTime>)  =
        dictionary 
        |> Seq.toList
        |> List.filter (fun keyValue -> keyValue.Value <= DateTime.Now.AddHours(-2.0))
        |> Pipe.tee (List.iter(fun keyValue -> dictionary.Remove(keyValue.Key) |> ignore))
        |> List.iter (fun keyValue -> log (MessageQueueConsumeFail $"Job id = {keyValue.Key} wasnt acknowledged for two hours"))

    let consumeActor (channel: IModel) (logError: LogErrorFunc) (handler: string -> Async<Result<unit, Errors>>) = MailboxProcessor.Start(fun (inbox: MailboxProcessor<BasicDeliverEventArgs>) ->
        let acknowledgeFailedTags = new Dictionary<uint64, DateTime>()
        let handle' = consumedHandler handler logError
        let log' = log logError

        let rec messageLoop cleanDate = async {
            let! msgOption = inbox.TryReceive(10000)
            match msgOption with
            | None -> ()
            | Some msg ->  match msg.Redelivered with
                           | true -> match acknowledgeFailedTags.ContainsKey(msg.DeliveryTag) with
                                     | true -> ()
                                     | false -> do! handle' msg.Body
                           | false -> do! handle' msg.Body
                           try
                                channel.BasicAck (msg.DeliveryTag, false)
                                match acknowledgeFailedTags.ContainsKey(msg.DeliveryTag) with
                                | false -> ()
                                | true -> acknowledgeFailedTags.Remove(msg.DeliveryTag) |> ignore          
                           with exn ->  match acknowledgeFailedTags.ContainsKey(msg.DeliveryTag) with
                                        | false -> acknowledgeFailedTags.Add(msg.DeliveryTag, DateTime.Now)
                                        | true -> ()
                                        log' (MessageQueueConsumeFailExn exn)
            match cleanDate >= DateTime.Now with
            | true -> return! messageLoop cleanDate
            | false -> handleLostMessages logError acknowledgeFailedTags
                       return! messageLoop (DateTime.Now.AddMinutes(2.0))                
        }
        messageLoop (DateTime.Now.AddMinutes(2.0))
        )

    let enqueue (actor: MailboxProcessor<_>) (message:BasicDeliverEventArgs) = 
        actor.Post message

    let getNewQueueConsumer (rabbitChannel: IModel) queue (logError: LogErrorFunc) (handler: string -> Async<Result<unit, Errors>>) = 
        let actor  = consumeActor rabbitChannel logError handler
        let enqueue' message = enqueue actor message
        let consumer = new EventingBasicConsumer(rabbitChannel)
        let eventHandler sender (message:BasicDeliverEventArgs) = 
            enqueue' message
        consumer.Received.AddHandler(new EventHandler<BasicDeliverEventArgs>(eventHandler))
        rabbitChannel.BasicConsume(queue, false, consumer) |> ignore