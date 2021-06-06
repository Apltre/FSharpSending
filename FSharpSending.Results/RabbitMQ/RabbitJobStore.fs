namespace FSharpSending.Results.RabbitMQ

open FSharpSending.Common.Types.CommonTypes
open RabbitMQ.Client
open FSharpSending.Common.Helpers.RabbitMQ
open FSharpSending.Results.Stores.JobMessageBus
open Logger

module RabbitJobStore =
    let getActor (channel: IModel) (log: LogErrorFunc) = MailboxProcessor.Start(fun inbox ->
        let queueJob = QueueHelper.queueJob channel log
        let rec messageLoop () = async {
            let! (queue, job) = inbox.Receive()
            queueJob queue job
            return! messageLoop ()
        }
        messageLoop ()
        )

    let enqueue (actor: MailboxProcessor<_>) queue job = 
        actor.Post (queue, job)

    let initializeRabbitQueues workflowId (rabbitChannel: IModel) () =
           let declareReliableChannelForWorkflow = QueueHelper.declareReliableChannel workflowId
           rabbitChannel
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getResultsToQueueName)
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getQueueToResultsName)
            |> ignore

    let createRabbitJobStore (connection : IConnection) (workflowId: WorkflowId) (logError: LogErrorFunc) =
        let actor = getActor (connection.CreateModel ()) logError

        {
            enqueueToQueue = ToQueueBusQueueFunc (enqueue actor (QueueNames.getResultsToQueueName workflowId)) 
            getResultsConsumer = GetSendingBusConsumerFunc (RabbitQueueConsumer.getNewQueueConsumer (connection.CreateModel ()) (QueueNames.getQueueToResultsName workflowId) logError)
            initializeQueues = initializeRabbitQueues workflowId (connection.CreateModel ()) 
        }