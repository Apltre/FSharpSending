namespace FSharpSending.Sending.Stores

open FSharpSending.Common.Types.CommonTypes

module JobMessageBus =
    type ToQueueBusQueueFunc = ToQueueBusQueueFunc of (Job -> unit)
    type GetSendingBusConsumerFunc = GetSendingBusConsumerFunc of ((string -> Result<unit, DomainError>) -> unit)

    type MessageBusStore = {
         enqueueToQueue: ToQueueBusQueueFunc
         getSendingConsumer: GetSendingBusConsumerFunc
         initializeQueues: (unit -> unit)
    }