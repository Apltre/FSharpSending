namespace FSharpSending.Common.Types

open System

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
 
  //  type DomainJob =
    //    | UnsentJob of UnsentJob
      //  | SentJob of SentJob 
      //  | UnhandledResultJob of UnhandledResultJob 
       // | HandledResultJob of HandledResultJob

    type DomainError =
        | Error of string
        | JsonSerializationFail of string
        | DbUpdateFailExn of Exception
        | DbQueryFailExn of Exception
        | MessageQueueFailExn of Exception
        | MessageQueueEnqueueFailExn of Exception
        | MessageQueueConsumeFailExn of Exception
        | MessageQueueConsumeFail of string

//open CommonTypes

//module JobConverter = 
//    let ofJson : Decoder<Job> =
 //       Decode.object(fun fields -> {
//            Id = fields.Optional.At ["id"] Decode.string
  ///          Data = fields.Optional.At ["data"] Decode.string
     //       Type = fields.Required.At ["sendingType"] (Decode.Auto.generateDecoder<SendingType> (CaseStrategy.SnakeCase))
     //       Status = fields.Optional.At ["status"] (Decode.Auto.generateDecoder<JobStatus> (CaseStrategy.SnakeCase))
        //    AttemptNumber = fields.Required.At ["attemptNumber"] Decode.int
      //      ExecuteMessage = fields.Optional.At ["executeMessage"] Decode.string
          //  StartTime = fields.Required.At ["startTime"] Decode.datetime
        //    CreateTime = fields.Required.At ["createTime"] Decode.datetime
 //           ProcessedDate = fields.Optional.At ["processedDate"] Decode.datetime
   //         ResultHandlingStatus = fields.Optional.At ["processedDate"] Decode.int
     //       ResultHandlingStartTime = fields.Optional.At ["resultHandlingStartTime"] Decode.datetime
       //     ResultHandlingAttemptIndex = fields.Required.At ["resultHandlingAttemptIndex"] Decode.int
         //   ResultHandlingException = fields.Optional.At ["resultHandlingException"] Decode.string
        //})
        