module JobProcessor

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Sending.Stores.JobMessageBus

    let processJob (insertInQueueQueue: ToQueueBusQueueFunc) (job: Job) =
        ()

