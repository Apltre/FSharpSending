namespace FSharpSending.Queue

open Logger
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores.DbJob
open FSharpSending.Common.Helpers.Signal
open System

module StaleJobsHandler =
    type StaleJobsCount = int

    let setJobsToPending job = 
        { job with SendingInfo = 
                    { job.SendingInfo with Status = JobStatus.Pending 
                                           StartTime = DateTime.Now }
        }

    let handleNewStaleJobs (GetStaleJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) () : Async<(StaleJobsCount * CompletedSignalAwaiter option)> =
        async {
            let! staleJobs = getJobs ()
            match (List.length staleJobs) > 0 with
            | true ->
                List.iter (fun (job) -> log $"Job Id = {job.Id.Value} was stale.") staleJobs
                let resetToPendingJobs = List.map setJobsToPending staleJobs
                let commitAwaiter = updateJobs resetToPendingJobs
                return (staleJobs.Length, Some commitAwaiter)
            | false -> return (0, None)
        }

    let setJobsToResultPending job = 
        { job with ResultHandlingInfo = 
                    { job.ResultHandlingInfo with ResultHandlingStatus = Some JobResultHandlingStatus.Pending
                                                  ResultHandlingStartDate = Some DateTime.Now }
        }

    let handleStaleResultJobs (GetStaleResultHandlingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) () : Async<(StaleJobsCount * CompletedSignalAwaiter option)> =
        async {
            let! staleResultJobs = getJobs ()
            match (List.length staleResultJobs) > 0 with
            | true ->
                List.iter (fun (job) -> log $"Job Id = {job.Id.Value} was stale.") staleResultJobs
                let resetToPendingResultProcessedJobs = List.map setJobsToResultPending staleResultJobs
                let commitAwaiter = updateJobs resetToPendingResultProcessedJobs
                return (staleResultJobs.Length, Some commitAwaiter)
            | false -> return (0, None)
        }

    let handleStaleJobs (jobStore: DbJobStore) (loggerFunc: LogInfoFunc) () =
        let handleJ = handleNewStaleJobs jobStore.getStaleJobs loggerFunc jobStore.updateJobs
        let handleR = handleStaleResultJobs jobStore.getStaleResultHandlingJobs loggerFunc jobStore.updateJobs
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