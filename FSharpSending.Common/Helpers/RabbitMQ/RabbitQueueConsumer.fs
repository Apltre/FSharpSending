namespace FSharpSending.Common.Helpers.RabbitMQ

open RabbitMQ.Client
open Logger
open FSharpSending.Common.Types.CommonTypes
open RabbitMQ.Client.Events
open System
open System.Text

module RabbitQueueConsumer =
    let getNewQueueConsumer (rabbitChannel: IModel) queue (LogErrorFunc logError) (handler: string -> Result<unit, DomainError>) = 
        let consumer = new EventingBasicConsumer(rabbitChannel)
        let eventHandler sender (data:BasicDeliverEventArgs) =
                    let body = data.Body.ToArray()
                    let message = Encoding.UTF8.GetString body
                    handler message |> Result.teeError logError
                                    |> ignore
                    rabbitChannel.BasicAck (data.DeliveryTag, false)
        consumer.Received.AddHandler(new EventHandler<BasicDeliverEventArgs>(eventHandler))
        rabbitChannel.BasicConsume(queue, false, consumer) |> ignore
