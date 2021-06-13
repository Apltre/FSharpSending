namespace FSharpSending.Results

open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open System.Threading
open Microsoft.Extensions.Configuration
open FSharpSending.Common.Types.CommonTypes
open Logger
open System
open FSharpSending.Results.Stores.JobMessageBus
open FSharpSending.Common.Helpers.Json

type ResultsService(configuration : IConfiguration, busStore: MessageBusStore, loggerStore: LoggerStore, serviceProvider: IServiceProvider) =  

    let initlogInfo (LogInfoFunc logInfo) message =
        logInfo message

    let sendingConsumer (GetSendingBusConsumerFunc sendConsumer) (serviceProvider : IServiceProvider) =
        let jobHandler = JobResultProcessor.processJob  busStore.enqueueToQueue serviceProvider
        let handler str = async {
            let handle' = JsonDecoder.decode >> (Result.bindToAsync jobHandler)
            return! handle' str
        }
        sendConsumer handler
        ()
    let service (loggerStore : LoggerStore) (busStore: MessageBusStore) (workflowId: WorkflowId) =
        let logInfo = initlogInfo (loggerStore.logMessage)
        logInfo $"Results service Id = {workflowId} started."
        busStore.initializeQueues ()      
        sendingConsumer busStore.getResultsConsumer serviceProvider

    interface IHostedService with
        member this.StartAsync (cancellationToken : CancellationToken) =
            async {
                let workflowId = WorkflowId (Int32.Parse configuration.["WorkflowId"])
                service loggerStore busStore workflowId
            }
            |> Async.StartAsTask :> Task

        member this.StopAsync (cancellationToken : CancellationToken) =
            Task.CompletedTask