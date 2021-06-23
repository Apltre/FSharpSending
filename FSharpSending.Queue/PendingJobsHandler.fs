namespace FSharpSending.Queue

open Logger
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores.JobMessageBus
open FSharpSending.Queue.Stores.DbJob
open FSharpSending.Common.Helpers.Signal

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

    let handleNewPendingJobs (GetPendingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) (ToSendingBusQueueFunc enqueue) () : Async<(HandledJobsCount * CompletedSignalAwaiter option)> =
        async {
            log "pending jobs selection"
            let! pendingJobs = getJobs ()
            match (List.length pendingJobs) > 0 with
            | true ->
                let beingProcessedJobs = List.map setJobToBeingProcessed pendingJobs
                let commitAwaiter = updateJobs beingProcessedJobs
                beingProcessedJobs |> List.iter enqueue
                return (pendingJobs.Length, Some commitAwaiter)
            | false -> return (0, None)
        }

    let handlePendingResultJobs (GetPendingResultHandlingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) (ToResultBusQueueFunc enqueue) () : Async<(HandledJobsCount * CompletedSignalAwaiter option)> =
        async {
            log "pending results selection"
            let! pendingResultJobs = getJobs ()
            match (List.length pendingResultJobs) > 0 with
            | true ->
                let beingResultProcessedJobs = List.map setJobToBeingResultProcessed pendingResultJobs
                let commitAwaiter = updateJobs beingResultProcessedJobs
                beingResultProcessedJobs |> List.iter enqueue
                return (pendingResultJobs.Length, Some commitAwaiter)
            | false -> return (0, None)
        }

    let handlePendingJobs (jobStore: DbJobStore) (busStore: MessageBusStore) (loggerFunc: LogInfoFunc) () =
        let handleJ = handleNewPendingJobs jobStore.getPendingJobs loggerFunc jobStore.updateJobs busStore.enqueueToSending
        let handleR = handlePendingResultJobs jobStore.getPendingResultHandlingJobs loggerFunc jobStore.updateJobs busStore.enqueueToResult
        let rec handle () = 
            async {
                let! (handledJobsCount, hjCommitAwaiter) = handleJ ()
                let! (handleResultJobsCount, hrjCommitAwaiter) = handleR ()

                match (handledJobsCount + handleResultJobsCount) > 0 with
                | true -> match hjCommitAwaiter with
                          | Some awaiter -> do! CompletedSignalModule.awaitCompleted awaiter
                          | None -> ()

                          match hrjCommitAwaiter with
                          | Some awaiter -> do! CompletedSignalModule.awaitCompleted awaiter
                          | None -> ()
                        
                | false -> do! Async.Sleep(1000)
                do! handle ()   
            }
        handle ()