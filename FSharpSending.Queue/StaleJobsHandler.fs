namespace FSharpSending.Queue

open Logger
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores.DbJob
open FSharpSending.Common.Helpers.Signal
open System

module StaleJobsHandler =
    type StaleJobsCount = int

    let handleNewStaleJobs (GetStaleJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) () : Async<(StaleJobsCount * CompletedSignalAwaiter)> =
        let setJobsToPending job = 
            { job with SendingInfo = 
                        { job.SendingInfo with Status = JobStatus.Pending 
                                               StartTime = DateTime.Now }
            }
        async {
            let! staleJobs = getJobs ()
            List.iter (fun (job) -> log $"Job Id = {job.Id.Value} was stale.") staleJobs
            let resetToPendingJobs = List.map setJobsToPending staleJobs
            let commitAwaiter = updateJobs resetToPendingJobs
            return (staleJobs.Length, commitAwaiter)
        }

    let handleStaleResultJobs (GetStaleResultHandlingJobsFunc getJobs) (LogInfoFunc log) (UpdateJobsFunc updateJobs) () : Async<(StaleJobsCount * CompletedSignalAwaiter)> =
        let setJobsToResultPending job = 
            { job with ResultHandlingInfo = 
                        { job.ResultHandlingInfo with ResultHandlingStatus = Some JobResultHandlingStatus.Pending
                                                      ResultHandlingStartDate = Some DateTime.Now }
            }
        async {
            let! staleResultJobs = getJobs ()
            List.iter (fun (job) -> log $"Job Id = {job.Id.Value} was stale.") staleResultJobs
            let resetToPendingResultProcessedJobs = List.map setJobsToResultPending staleResultJobs
            let commitAwaiter = updateJobs resetToPendingResultProcessedJobs
            return (staleResultJobs.Length, commitAwaiter)
        }

    let handleStaleJobs (jobStore: DbJobStore) (loggerFunc: LogInfoFunc) () =
        let handleJ = handleNewStaleJobs jobStore.getStaleJobs loggerFunc jobStore.updateJobs
        let handleR = handleStaleResultJobs jobStore.getStaleResultHandlingJobs loggerFunc jobStore.updateJobs
        let rec handle () = 
            async {
                let! (handledJobsCount, hjCommitAwaiter) = handleJ ()
                let! (handleResultJobsCount, hrjCommitAwaiter) = handleR ()

                match (handledJobsCount + handleResultJobsCount) > 0 with
                | true -> do! CompletedSignalModule.awaitCompleted hjCommitAwaiter
                          do! CompletedSignalModule.awaitCompleted hrjCommitAwaiter
                | false -> do! Async.Sleep(1000)
                do! handle ()   
            }
        handle ()