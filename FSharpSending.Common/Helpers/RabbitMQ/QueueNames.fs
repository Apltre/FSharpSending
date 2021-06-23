namespace FSharpSending.Common.Helpers.RabbitMQ

open FSharpSending.Common.Types.CommonTypes

module QueueNames =
    let getResultsToQueueName (WorkflowId workflowId) =
        $"sending_results_queue_{workflowId}"

    let getQueueToResultsName (WorkflowId workflowId) =
        $"sending_queue_results_{workflowId}"
    
    let getSendingToQueueName (WorkflowId workflowId) = 
        $"sending_sending_queue_{workflowId}"

    let getQueueToSendingName (WorkflowId workflowId) = 
        $"sending_queue_sending_{workflowId}"