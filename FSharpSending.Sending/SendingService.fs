namespace FSharpSending.Sending

open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open System.Threading
open Microsoft.Extensions.Configuration
open FSharpSending.Common.Types.CommonTypes
open Logger
open System
open FSharpSending.Sending.Stores.JobMessageBus
open FSharpSending.Common.Helpers.Json

type SendingService(configuration : IConfiguration, busStore: MessageBusStore, loggerStore: LoggerStore, serviceProvider: IServiceProvider) =  

    let initlogInfo (LogInfoFunc logInfo) message =
        logInfo message

    let sendingConsumer (GetSendingBusConsumerFunc sendConsumer) (serviceProvider : IServiceProvider) =
        let jobHandler = JobProcessor.processJob  busStore.enqueueToQueue serviceProvider
        let handler str = async {
            try
                //let handler = JsonDecoder.decode >> (Result.bindToAsync jobHandler)
                let job = JsonDecoder.decode str
                return! job |> Result.bindToAsync jobHandler
            with ex -> return (Ok ())
        }
        sendConsumer handler
        ()
    let service (loggerStore : LoggerStore) (busStore: MessageBusStore) (workflowId: WorkflowId) =
        let logInfo = initlogInfo (loggerStore.logMessage)
        logInfo $"Service Id = {workflowId} started."
        busStore.initializeQueues ()      
        sendingConsumer busStore.getSendingConsumer serviceProvider

    interface IHostedService with
        member this.StartAsync (cancellationToken : CancellationToken) =
            async {
                let workflowId = WorkflowId (Int32.Parse configuration.["WorkflowId"])
                service loggerStore busStore workflowId
            }
            |> Async.StartAsTask :> Task

        member this.StopAsync (cancellationToken : CancellationToken) =
            Task.CompletedTask