namespace FSharpSending.Common.Types

open System
open Thoth.Json.Net

module CommonTypes =
    type JobStatus =
        | FinishedSuccessfully = 2
        | BeingProcessed = 1
        | Pending = 0
        | FatalError = -1
        | ResendableError = -2
        | UnresendableError = -3

    type SendingType =
        | Agents_SendOrder = 1000
        | Agents_SendCancel = 1001

    type JobResultHandlingStatus =
        | FinishedSuccessfully = 20
        | BeingProcessed = 10
        | Pending = 0
        | Failed = -10
        | FatalError = -20

    type AttemptNumber = AttemptNumber of uint

    module AttemptNumberModule =
           let toInt (AttemptNumber x) =
               int x

           let ofInt (x : int) =
               AttemptNumber (uint x)

           let increment (AttemptNumber x) =
                AttemptNumber (x + 1u)

    type AttemptNumber with       
        static member (+) ((AttemptNumber x), y) =
            AttemptNumber (x + y)

    type JobId = JobId of string
    type WorkflowId = WorkflowId of int

    module JobId =
        let unwrapOfOption id =
                   match id with
                       | Some (JobId x) -> x
                       | None -> null

        let wrapToOption id =
            match id with
                | null ->  None
                | _ -> Some (JobId id)

    type SendingInfo = {
        Data : string option
        Type : SendingType
        Status : JobStatus
        AttemptNumber : AttemptNumber
        Message : string option
        CreateTime : DateTime
        StartTime : DateTime
        ProcessedDate : DateTime option
    }

    type ResultHandlingInfo = {
        ResultHandlingAttemptNumber : AttemptNumber
        ResultHandlingStatus : JobResultHandlingStatus option
        ResultHandlingStartDate : DateTime option
        ResultHandlingProcessedDate : DateTime option
        ResultHandlingMessage : string option
    }

    type ResultHandlingInfo with
        static member Default = 
            {
                ResultHandlingAttemptNumber = 0 |> AttemptNumberModule.ofInt
                ResultHandlingStatus  = None
                ResultHandlingStartDate = None
                ResultHandlingProcessedDate = None
                ResultHandlingMessage = None 
            }
     
    type Job = { 
        Id : JobId option
        SendingInfo : SendingInfo
        ResultHandlingInfo : ResultHandlingInfo
    }

    type SentJob = SentJob of Job
    type UnsentJob = UnsentJob of Job
    type UnhandledResultJob = UnhandledResultJob of Job
    type HandledResultJob = HandledResultJob of Job

    type DomainError =
        | Error of string
        | ErrorExn of Exception
        | JsonSerializationFail of string
        | DbUpdateFailExn of Exception
        | DbQueryFailExn of Exception
        | MessageQueueFailExn of Exception
        | MessageQueueEnqueueFailExn of Exception
        | MessageQueueConsumeFailExn of Exception
        | MessageQueueConsumeFail of string

open CommonTypes


//Type : SendingType
//Status : JobStatus
//AttemptNumber : AttemptNumber

module SendingInfoConverter = 
    let ofJson : Decoder<SendingInfo> =
        Decode.object(fun fields -> { 
            Data = fields.Optional.At ["Data"] Decode.string
            Type = fields.Required.At ["Type"] (Decode.Auto.generateDecoder<SendingType> (CaseStrategy.PascalCase))
            Status = fields.Required.At ["Status"] (Decode.Auto.generateDecoder<JobStatus> (CaseStrategy.PascalCase))
            AttemptNumber = fields.Required.At ["AttemptNumber"] (Decode.Auto.generateDecoder<AttemptNumber> (CaseStrategy.PascalCase))
            Message = fields.Optional.At ["Message"] Decode.string
            CreateTime = fields.Required.At ["CreateTime"] Decode.datetime
            StartTime = fields.Required.At ["CreateTime"] Decode.datetime
            ProcessedDate = fields.Optional.At ["ProcessedDate"] Decode.datetime
        })

//ResultHandlingAttemptNumber : AttemptNumber
//ResultHandlingStatus : JobResultHandlingStatus option

module ResultHandlingInfoConverter = 
    let ofJson : Decoder<ResultHandlingInfo> =
        Decode.object(fun fields -> { 
            ResultHandlingAttemptNumber = fields.Required.At ["ResultHandlingAttemptNumber"] (Decode.Auto.generateDecoder<AttemptNumber> (CaseStrategy.PascalCase))
            ResultHandlingStatus = fields.Optional.At ["ResultHandlingStatus"] (Decode.Auto.generateDecoder<JobResultHandlingStatus> (CaseStrategy.PascalCase))
            ResultHandlingStartDate = fields.Optional.At ["ResultHandlingStartDate"] Decode.datetime
            ResultHandlingProcessedDate = fields.Optional.At ["ResultHandlingProcessedDate"] Decode.datetime
            ResultHandlingMessage = fields.Optional.At ["ResultHandlingMessage"] Decode.string
        })

module JobConverter = 
    let ofJson : Decoder<Job> =
        Decode.object(fun fields -> {
            Id = fields.Optional.At ["Id"] (Decode.Auto.generateDecoder<JobId> (CaseStrategy.PascalCase))
            SendingInfo = fields.Required.At ["SendingInfo"] SendingInfoConverter.ofJson
            ResultHandlingInfo = fields.Required.At ["ResultHandlingInfo"] ResultHandlingInfoConverter.ofJson
        })
        