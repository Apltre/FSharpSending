namespace FSharpSending.Queue.Stores

open FSharpSending.Common.Types.CommonTypes

module JobMessageBus =
    type ToResultBusQueueFunc = ToResultBusQueueFunc of (Job -> unit)
    type ToSendingBusQueueFunc = ToSendingBusQueueFunc of (Job -> unit)
    type GetResultsBusConsumerFunc = GetResultsBusConsumerFunc of ((string -> Result<unit, DomainError>) -> unit)
    type GetSendingBusConsumerFunc = GetSendingBusConsumerFunc of ((string -> Result<unit, DomainError>) -> unit)

    type MessageBusStore = {
         enqueueToResult: ToResultBusQueueFunc
         enqueueToSending: ToSendingBusQueueFunc
         getResultsConsumer: GetResultsBusConsumerFunc
         getSendingConsumer: GetSendingBusConsumerFunc
         initializeQueues: (unit -> unit)
     }