namespace FSharpSending.Queue.RabbitMQ

open FSharpSending.Common.Types.CommonTypes
open RabbitMQ.Client
open FSharpSending.Common.Helpers.RabbitMQ
open FSharpSending.Queue.Stores.JobMessageBus
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
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getSendingToQueueName)
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getQueueToSendingName)
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getQueueToResultsName)
            |> Pipe.tee (declareReliableChannelForWorkflow QueueNames.getResultsToQueueName)
            |> ignore

    let createRabbitJobStore (connection : IConnection) (workflowId: WorkflowId) (logError: LogErrorFunc) =
        let actor = getActor (connection.CreateModel ()) logError

        {
            enqueueToResult = ToResultBusQueueFunc (enqueue actor (QueueNames.getQueueToResultsName workflowId)) 
            enqueueToSending = ToSendingBusQueueFunc (enqueue actor (QueueNames.getQueueToSendingName workflowId))
            getResultsConsumer = GetResultsBusConsumerFunc (RabbitQueueConsumer.getNewQueueConsumer (connection.CreateModel ()) (QueueNames.getResultsToQueueName workflowId) logError)
            getSendingConsumer = GetSendingBusConsumerFunc (RabbitQueueConsumer.getNewQueueConsumer (connection.CreateModel ()) (QueueNames.getSendingToQueueName workflowId) logError)
            initializeQueues = initializeRabbitQueues workflowId (connection.CreateModel ()) 
        }