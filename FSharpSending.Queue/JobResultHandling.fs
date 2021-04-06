module JobResultHandling

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores
open System

    type ValidStatusSendingInfo = ValidStatusSendingInfo of SendingInfo
    type ValidSendingResultStatusInfo = {
        ResultHandlingAttemptNumber : AttemptNumber
        ResultHandlingStatus : JobResultHandlingStatus
        ResultHandlingStartDate : DateTime option
        ResultHandlingProcessedDate : DateTime option
        ResultHandlingMessage : string option
    }

    type ValidatedResultHandlingJob = { 
        Id : JobId option
        ValidStatusSendingInfo : ValidStatusSendingInfo
        ValidSendingResultStatusInfo : ValidSendingResultStatusInfo
    }

    type PrepairedJob = PrepairedJob of ValidatedResultHandlingJob

    module ValidStatusSendingInfoModule =
        let ofInfo jobSendingInfo =
            ValidStatusSendingInfo jobSendingInfo
        let unwrap (ValidStatusSendingInfo info) = 
            info

    module ValidatedResultHandlingInfoModule =
        let ofInfo (jobResultInfo : ResultHandlingInfo) =
            {
                ResultHandlingAttemptNumber = jobResultInfo.ResultHandlingAttemptNumber
                ResultHandlingStatus = jobResultInfo.ResultHandlingStatus.Value
                ResultHandlingStartDate = jobResultInfo.ResultHandlingStartDate
                ResultHandlingProcessedDate =jobResultInfo.ResultHandlingProcessedDate
                ResultHandlingMessage = jobResultInfo.ResultHandlingMessage
            }

        let toInfo (jobResultInfo : ValidSendingResultStatusInfo) : ResultHandlingInfo =
            {
                ResultHandlingAttemptNumber = jobResultInfo.ResultHandlingAttemptNumber
                ResultHandlingStatus =  Some (jobResultInfo.ResultHandlingStatus)
                ResultHandlingStartDate = jobResultInfo.ResultHandlingStartDate
                ResultHandlingProcessedDate =jobResultInfo.ResultHandlingProcessedDate
                ResultHandlingMessage = jobResultInfo.ResultHandlingMessage
            }

    module ValidatedResultHandlingJobModule =
        let toJob (job : ValidatedResultHandlingJob) =
            {
                Id = job.Id
                SendingInfo = ValidStatusSendingInfoModule.unwrap job.ValidStatusSendingInfo
                ResultHandlingInfo = ValidatedResultHandlingInfoModule.toInfo job.ValidSendingResultStatusInfo
            }


    let handleBadJobStatus jobStatusInfo =
                match jobStatusInfo.Status with
                           | JobStatus.Pending
                           | JobStatus.BeingProcessed ->
                               let validatedInfo = { jobStatusInfo with Status = JobStatus.FatalError
                                                                        Message = Some ($"Job received from result handling with {jobStatusInfo.Status} status") }
                               ValidStatusSendingInfoModule.ofInfo validatedInfo
                           | _ -> ValidStatusSendingInfoModule.ofInfo jobStatusInfo
        
    let handleBadJobResultHandlingStatus (jobResultStatusInfo : ResultHandlingInfo) =
        let fatalResultJob message = 
            let validatedInfo = { jobResultStatusInfo with ResultHandlingStatus = Some (JobResultHandlingStatus.FatalError)
                                                           ResultHandlingMessage = Some (message) }
            ValidatedResultHandlingInfoModule.ofInfo validatedInfo

        match jobResultStatusInfo.ResultHandlingStatus with
            | Some rs -> match rs with
                                | JobResultHandlingStatus.Pending
                                | JobResultHandlingStatus.BeingProcessed -> 
                                    fatalResultJob $"Job received with {jobResultStatusInfo.ResultHandlingStatus} result handling status"
                                | _ -> ValidatedResultHandlingInfoModule.ofInfo jobResultStatusInfo
            | None -> fatalResultJob $"Job received with empty result handling status"

    let validateJob (HandledResultJob job) =
        {
            Id = job.Id
            ValidStatusSendingInfo = handleBadJobStatus job.SendingInfo
            ValidSendingResultStatusInfo = handleBadJobResultHandlingStatus job.ResultHandlingInfo
        }

    let handleJobResultHandling (job : ValidatedResultHandlingJob) =
        let info = job.ValidSendingResultStatusInfo
        match info.ResultHandlingStatus with
            | JobResultHandlingStatus.Failed ->
                match info.ResultHandlingAttemptNumber with
                    | AttemptNumber m when m = 10u -> PrepairedJob { job with ValidSendingResultStatusInfo = 
                                                                              { info with ResultHandlingStatus = JobResultHandlingStatus.Failed }
                                         }
                    | _  -> PrepairedJob { job with ValidSendingResultStatusInfo = 
                                                    { info with ResultHandlingStatus = JobResultHandlingStatus.Pending
                                                                ResultHandlingAttemptNumber = AttemptNumberModule.increment info.ResultHandlingAttemptNumber
                                                                ResultHandlingStartDate = Some (CommonHelper.getStartDelay info.ResultHandlingAttemptNumber) }
                                         }
            | _ -> PrepairedJob job

    let updateJob (UpdateJobFunc update) (PrepairedJob job) =
        let job = ValidatedResultHandlingJobModule.toJob job
        update job

    let handleResultJob (update : UpdateJobFunc) (job : HandledResultJob)  =
        job |> validateJob 
            |> handleJobResultHandling
            |> updateJob update
            |> Ok