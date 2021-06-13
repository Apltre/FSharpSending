namespace FSharpSending.Queue.Mongo

open System
open FSharpSending.Common.Types.CommonTypes
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open MongoDB.Bson.Serialization.IdGenerators

type MongoJob = 
    {
        [<BsonId (IdGenerator = typeof<StringObjectIdGenerator>)>]
        Id : string
        WorkflowId: int
        Data : BsonDocument
        Type : SendingType
        Status : JobStatus
        AttemptNumber : int
        Message : string
        StartTime : DateTime
        CreateTime : DateTime;
        ProcessedDate : Nullable<DateTime>
        ResultHandlingStatus : Nullable<JobResultHandlingStatus>
        ResultHandlingStartDate : Nullable<DateTime>
        ResultHandlingAttemptNumber : int
        ResultHandlingMessage : string
    }

module MongoJobModule =
    let toMongoJob (job : Job)  =
        let jsonToBsonDocument data parser=
            match data with
                | None -> None
                | Some x -> Some (parser x)

        let sendingInfo = job.SendingInfo
        let resultsInfo = job.ResultHandlingInfo

        {
            Id =  job.Id |> JobId.unwrapOfOption
            WorkflowId = job.WorkflowId |> WorkflowIdModule.toInt
            MongoJob.Data = (jsonToBsonDocument sendingInfo.Data BsonDocument.Parse) |> Option.toObj
            Type = sendingInfo.Type
            Status = sendingInfo.Status
            AttemptNumber = sendingInfo.AttemptNumber |> AttemptNumberModule.toInt
            Message = sendingInfo.Message |> Option.toObj
            StartTime = sendingInfo.StartTime
            CreateTime = sendingInfo.CreateTime
            ProcessedDate = sendingInfo.ProcessedDate |> Option.toNullable
            ResultHandlingStatus = resultsInfo.ResultHandlingStatus |> Option.toNullable
            ResultHandlingStartDate = resultsInfo.ResultHandlingStartDate |> Option.toNullable
            ResultHandlingAttemptNumber = resultsInfo.ResultHandlingAttemptNumber |> AttemptNumberModule.toInt
            ResultHandlingMessage = resultsInfo.ResultHandlingMessage |> Option.toObj
        }

    let ofMongoJob (job : MongoJob) =
        let bsonDocumentToJson (data : BsonDocument option) =
            match data with
                | None -> None
                | Some x -> Some (x.ToJson())
        {
            Id = job.Id |> JobId.wrapToOption
            WorkflowId = job.WorkflowId |> WorkflowIdModule.ofInt
            SendingInfo = 
                {
                    Data = bsonDocumentToJson (job.Data |> Option.ofObj)
                    Type = job.Type
                    Status = job.Status
                    AttemptNumber = job.AttemptNumber |> AttemptNumberModule.ofInt
                    Message = job.Message |>  Option.ofObj
                    CreateTime = job.CreateTime
                    StartTime = job.StartTime
                    ProcessedDate = job.ProcessedDate |> Option.ofNullable
                }
            ResultHandlingInfo = 
                {
                    ResultHandlingStatus = job.ResultHandlingStatus |> Option.ofNullable
                    ResultHandlingStartDate = job.ResultHandlingStartDate |> Option.ofNullable
                    ResultHandlingProcessedDate = job.ResultHandlingStartDate |> Option.ofNullable
                    ResultHandlingAttemptNumber = job.ResultHandlingAttemptNumber |> AttemptNumberModule.ofInt
                    ResultHandlingMessage = job.ResultHandlingMessage |> Option.ofObj
                }
        }