module SentJobHandling

open FSharpSending.Common.Types.CommonTypes
open System
open FSharpSending.Queue.Stores.JobMessageBus
open FSharpSending.Queue.Stores.DbJob
open FSharpSending.Common.Helpers.Signal
open MongoDB.Bson

    type ValidStatusSendingInfoJob =  ValidStatusSendingInfoJob of Job
    type PrepairedJob = PrepairedJob of Job

    let handleBadJobStatus (SentJob job) =
        match job.SendingInfo.Status with
        | JobStatus.Pending
        | JobStatus.BeingProcessed ->
            let info = job.SendingInfo
            let sendInfo = { info with  Status = JobStatus.FatalError
                                        Message = Some ($"Job received with {info.Status} status") }
            ValidStatusSendingInfoJob ({ job with SendingInfo = sendInfo})
        | _ -> ValidStatusSendingInfoJob job

    let prepareForResultHandling (ValidStatusSendingInfoJob job) =
               let resultInfo = job.ResultHandlingInfo
               PrepairedJob { job with ResultHandlingInfo = { resultInfo with ResultHandlingStatus = Some(JobResultHandlingStatus.BeingProcessed)
                                                                              ResultHandlingStartDate = Some(DateTime.Now) 
                                                            }
                            }

    let updateJob (UpdateJobFunc dbUpdate) (PrepairedJob job) = async {
        let awaiter = 
            job
            |> dbUpdate 
            |> CompletedSignalModule.awaitCompleted
        return! awaiter
    }

    let createNewJobAndPersistIfNeeded (AddJobFunc dbAdd) (PrepairedJob job) =
        let sendingInfo = job.SendingInfo
        match sendingInfo.Status with
        | JobStatus.ResendableError ->
            let attemptNumber = AttemptNumberModule.increment job.SendingInfo.AttemptNumber
            let newJob = { job with Id = Some (JobId (ObjectId.GenerateNewId().ToString()))
                                    SendingInfo = { sendingInfo with Status = JobStatus.Pending
                                                                              Message = None
                                                                              AttemptNumber = attemptNumber
                                                                              CreateTime = DateTime.Now
                                                                              StartTime = CommonHelper.getStartDelay attemptNumber
                                                                              ProcessedDate = None
                                                    }
                                    ResultHandlingInfo = ResultHandlingInfo.Default
                            }
            dbAdd newJob
        | _ -> ()

    let enqueueForResultHandling (ToResultBusQueueFunc insertInResultQueue) (PrepairedJob job) =
        insertInResultQueue job

    let handleSentJob (insertInResultQueue: ToResultBusQueueFunc) (insertInDb: AddJobFunc) (updateInDb: UpdateJobFunc) (job: SentJob)  = async {
        let prepairedJob = 
            job 
            |> handleBadJobStatus 
            |> PipeAsync.switch prepareForResultHandling
            |> PipeAsync.tee (updateJob updateInDb)
            |> PipeAsync.teeSync (enqueueForResultHandling insertInResultQueue)
            |> PipeAsync.teeSync (createNewJobAndPersistIfNeeded insertInDb)
            |> PipeAsync.mapUnit
        do! prepairedJob
    }