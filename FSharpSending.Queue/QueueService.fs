namespace FSharpSending.Queue

open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open System.Threading
open Microsoft.Extensions.Configuration
open FSharpSending.Common.Types.CommonTypes
open Logger
open System
open FSharpSending.Queue.Stores.JobMessageBus
open FSharpSending.Common.Helpers.Json
open FSharpSending.Queue.Stores.DbJob

type QueueService(configuration : IConfiguration, busStore: MessageBusStore, jobStore: DbJobStore, loggerStore: LoggerStore) =  

    let initlogInfo (LogInfoFunc logInfo) message =
        logInfo message

    let sentConsumer (GetSendingBusConsumerFunc sentConsumer) (jobStore : DbJobStore) =
         let sentJobHandler = SentJobHandling.handleSentJob  busStore.enqueueToResult jobStore.addJob  jobStore.updateJob
         let handlerAsync message = async {
            let handler = JsonDecoder.decode 
                          >> Result.map SentJob 
                          >> ResultAsync.teeToAsync sentJobHandler
                          >> ResultAsync.mapUnit
            return! handler message
         }
         sentConsumer handlerAsync

    let resultConsumer (GetResultsBusConsumerFunc sentConsumer) (updateJob: UpdateJobFunc) =
         let resultJobHandler = JobResultHandling.handleResultJob  updateJob
         let handlerAsync message = async {
            let handler = JsonDecoder.decode
                          >> Result.map HandledResultJob 
                          >> ResultAsync.teeToAsync resultJobHandler
                          >> ResultAsync.mapUnit
            return! handler message
         }
         sentConsumer handlerAsync

    let service (loggerStore : LoggerStore) (busStore: MessageBusStore) (jobStore : DbJobStore) (workflowId: WorkflowId) =
        let logInfo = initlogInfo (loggerStore.logMessage)
        logInfo $"Service Id = {workflowId} started."
        busStore.initializeQueues ()
        
        sentConsumer busStore.getSendingConsumer jobStore
       // resultConsumer busStore.getResultsConsumer jobStore.updateJob
        //StaleJobsHandler.handleStaleJobs jobStore loggerStore.logMessage () |> Async.Start
        //PendingJobsHandler.handlePendingJobs jobStore busStore loggerStore.logMessage () |> Async.RunSynchronously
        Async.Sleep(1000000000) |> Async.RunSynchronously

    interface IHostedService with
        member this.StartAsync (cancellationToken : CancellationToken) =
            async {
                let workflowId = WorkflowId (Int32.Parse configuration.["WorkflowId"])
                service loggerStore busStore jobStore workflowId
            }
            |> Async.StartAsTask :> Task

        member this.StopAsync (cancellationToken : CancellationToken) =
            Task.CompletedTask