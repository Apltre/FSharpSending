namespace FSharpSending.Queue

open Logger
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores.JobMessageBus
open FSharpSending.Queue.Stores.DbJob

module PendingJobsHandler =
    type HandledJobsCount = int

    let setJobToBeingProcessed job = 
        { job with SendingInfo = 
                   { job.SendingInfo with Status = JobStatus.BeingProcessed }
        }

    let setJobToBeingResultProcessed job = 
        { job with ResultHandlingInfo = 
                   { job.ResultHandlingInfo with ResultHandlingStatus = Some JobResultHandlingStatus.BeingProcessed }
        }

    let handleNewPendingJobs (GetPendingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) (ToSendingBusQueueFunc enqueue) () : Async<HandledJobsCount> =
        async {
            log "pending jobs selection"
            let! pendingJobs = getJobs ()
            let beingProcessedJobs = List.map setJobToBeingProcessed pendingJobs
            updateJobs beingProcessedJobs
            beingProcessedJobs |> List.iter enqueue
            return pendingJobs.Length
        }

    let handlePendingResultJobs (GetPendingResultHandlingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) (ToResultBusQueueFunc enqueue) () : Async<HandledJobsCount> =
        async {
            log "pending results selection"
            let! pendingResultJobs = getJobs ()
            let beingResultProcessedJobs = List.map setJobToBeingResultProcessed pendingResultJobs
            updateJobs beingResultProcessedJobs
            beingResultProcessedJobs |> List.iter enqueue
            return pendingResultJobs.Length
        }

    let handlePendingJobs (jobStore: DbJobStore) (busStore: MessageBusStore) (loggerFunc: LogInfoFunc) () =
        let handleJ = handleNewPendingJobs jobStore.getPendingJobs loggerFunc jobStore.updateJobs busStore.enqueueToSending
        let handleR = handlePendingResultJobs jobStore.getPendingResultHandlingJobs loggerFunc jobStore.updateJobs busStore.enqueueToResult
        let rec handle () = 
            async {
                let! handledJobsCount = handleJ ()
                let! handleResultJobsCount = handleR ()

                match (handledJobsCount + handleResultJobsCount) > 0 with
                | true -> ()
                | false -> do! Async.Sleep(100)
                do! handle ()   
            }
        handle ()