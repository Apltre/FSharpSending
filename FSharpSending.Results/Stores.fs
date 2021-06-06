namespace FSharpSending.Results.Stores

open FSharpSending.Common.Types.CommonTypes

module JobMessageBus =
    type ToQueueBusQueueFunc = ToQueueBusQueueFunc of (Job -> unit)
    type GetResultsBusConsumerFunc = GetSendingBusConsumerFunc of ((string -> Async<Result<unit, DomainError>>) -> unit)

    type MessageBusStore = {
         enqueueToQueue: ToQueueBusQueueFunc
         getResultsConsumer: GetResultsBusConsumerFunc
         initializeQueues: (unit -> unit)
    }