namespace FSharpSending.Common.Helpers.RabbitMQ

open RabbitMQ.Client
open FSharpSending.Common.Types.CommonTypes
open System.Text.Json
open System.Text
open System
open Logger

module QueueHelper =
    let declareReliableChannel (workflowId : WorkflowId) queueNameFun (channel : IModel) =
        let queueName = queueNameFun workflowId
        channel.QueueDeclare(queueName, true, false, false) |> ignore

    let queueJob (rabbitChannel : IModel) (LogErrorFunc log) queue (job: Job) =
        let json = JsonSerializer.Serialize job
        let jsonBytes = Encoding.UTF8.GetBytes(json);
        try
            rabbitChannel.BasicPublish("", queue, null, ReadOnlyMemory(jsonBytes))
        with exn -> log (MessageQueueEnqueueFailExn exn)