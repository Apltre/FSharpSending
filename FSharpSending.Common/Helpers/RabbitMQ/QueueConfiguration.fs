namespace FSharpSending.Common.Helpers.RabbitMQ

open RabbitMQ.Client
open FSharpSending.Common.Types.CommonTypes
open System.Text.Json
open System.Text
open System

module QueueHelper =
    let declareReliableChannel (workflowId : WorkflowId) queueNameFun (channel : IModel) =
        let queueName = queueNameFun workflowId
        channel.QueueDeclare(queueName, true, false, false) |> ignore

    let queueJob (rabbitChannel : IModel) queue (job: Job) =
        let json = JsonSerializer.Serialize job
        let jsonBytes = Encoding.UTF8.GetBytes(json);
        rabbitChannel.BasicPublish("", queue, null, ReadOnlyMemory(jsonBytes))