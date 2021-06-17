namespace FSharpSending.Queue.Mongo

open MongoDB.Driver
open System
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores.DbJob
open FSharpSending.Common.Helpers.Signal
open System.Collections.Generic

module MongoJobStore =
    let staleBorderTime () = DateTime.Now.AddHours(-2.0)

    let getPendingJobs (jobsCollection: IMongoCollection<MongoJob>) (WorkflowId workflowId) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> job.Status = JobStatus.Pending 
                                                                 && job.StartTime <= DateTime.Now
                                                                 && job.StartTime > staleBorderTime ()
                                                                 && job.WorkflowId = workflowId)
                                      .SortBy(fun job -> job.Id :> Object)
                                      .Limit(100)
                                      .ToListAsync() |> Async.AwaitTask
            return jobs  |> Seq.toList
                         |> List.map MongoJobModule.ofMongoJob
        }   

    let getPendingResultHandlingJobs (jobsCollection: IMongoCollection<MongoJob>) (WorkflowId workflowId) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> job.ResultHandlingStatus.Value = JobResultHandlingStatus.Pending 
                                                                 && job.ResultHandlingStartDate.Value <= DateTime.Now
                                                                 && job.ResultHandlingStartDate.Value > staleBorderTime ()
                                                                 && job.WorkflowId = workflowId)
                                      .SortBy(fun job -> job.Id :> Object)
                                      .Limit(100)
                                      .ToListAsync() |> Async.AwaitTask
            return jobs |> Seq.toList
                        |> List.map MongoJobModule.ofMongoJob 
        }        

    let getStaleJobs (jobsCollection: IMongoCollection<MongoJob>) (WorkflowId workflowId) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> (job.Status = JobStatus.BeingProcessed ||  job.Status = JobStatus.Pending)
                                                                 && job.StartTime <= staleBorderTime ()
                                                                 && job.WorkflowId = workflowId)
                                      .SortBy(fun job -> job.Id :> Object)
                                      .Limit(100)
                                      .ToListAsync() |> Async.AwaitTask
            return jobs  |> Seq.toList
                         |> List.map MongoJobModule.ofMongoJob
        }   

    let getStaleResultHandlingJobs (jobsCollection: IMongoCollection<MongoJob>) (WorkflowId workflowId) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> (job.ResultHandlingStatus.Value = JobResultHandlingStatus.BeingProcessed || job.ResultHandlingStatus.Value = JobResultHandlingStatus.Pending)
                                                                 && job.ResultHandlingStartDate.Value <= staleBorderTime ()
                                                                 && job.WorkflowId = workflowId)
                                      .SortBy(fun job -> job.Id :> Object)
                                      .Limit(100)
                                      .ToListAsync() |> Async.AwaitTask
            return jobs |> Seq.toList
                        |> List.map MongoJobModule.ofMongoJob 
        }    

    let addJobs (jobsCollection: IMongoCollection<MongoJob>) (jobs : Job list) = 
        async {
            let mongoJobs = jobs |> List.map MongoJobModule.toMongoJob
                                 |> List.toSeq
            do! jobsCollection.InsertManyAsync mongoJobs |> Async.AwaitTask
        }

    let updateJobs (jobsCollection: IMongoCollection<MongoJob>) (jobs : Job list) =
        async {
            let toReplaceOneModel (job : MongoJob) =  
                let filter = Builders<MongoJob>.Filter.Eq ((fun j -> j.Id), job.Id)
                ReplaceOneModel (filter , job)

            let updateBulk = jobs 
                             |> List.map MongoJobModule.toMongoJob
                             |> List.map toReplaceOneModel
                             |> List.toSeq
                             |> Seq.cast<WriteModel<MongoJob>>
            let! result = jobsCollection.BulkWriteAsync updateBulk |> Async.AwaitTask
            return ()
        }
          
    let persistActor (persist: Job list -> Async<unit>) = MailboxProcessor.Start(fun inbox ->
        let minimalWaitMs = 50
        let maxBatchSize = 150 // between 100..1000
        let getNewPersistDate () = DateTime.Now.Add (TimeSpan.FromMilliseconds 10.0)

        let mapToList (map: Dictionary<JobId option, Job>) =
            map 
            |> Seq.map (fun keyValue -> keyValue.Value)
            |> Seq.toList

        let rec addRange (jobs: Job list) (map: Dictionary<JobId option, Job>)  =
            match jobs with
            | x::xs -> map.[x.Id] <- x
                       addRange xs map
            | [] -> map

        let rec messageLoop (jobsMap: Dictionary<JobId option, Job>) awaitersList persistDate waitMultiplier = async {
            let waitTime = minimalWaitMs * waitMultiplier
            let! msgOption = inbox.TryReceive(waitTime)
            match msgOption with
            | Some ((jobs: Job list), (commitAwaiter: EmitCompletedSignalFunc option)) ->
                let newAwaitersList =
                    match commitAwaiter with
                    | Some awaiter -> awaiter :: awaitersList
                    | None -> awaitersList
                let newJobsMap = addRange jobs jobsMap 
                match (persistDate > DateTime.Now) && (jobsMap.Count < maxBatchSize) with
                | true -> return! messageLoop newJobsMap newAwaitersList persistDate 1
                | false -> let listToSave = newJobsMap |> mapToList
                           do! persist listToSave                       
                           newAwaitersList |> List.iter (fun (EmitCompletedSignalFunc signalFunc) -> signalFunc ())
                           return! messageLoop (new Dictionary<JobId option, Job>()) List.empty (getNewPersistDate ()) 1
                               
            | None -> match jobsMap.Count <> 0 with
                        | true -> do! persist (mapToList jobsMap)
                                  awaitersList |> List.iter (fun (EmitCompletedSignalFunc signalFunc) -> signalFunc ())
                        | false -> ()
            let getNewMultiplier x = 
                match x < 5 with
                | true -> x + 1
                | false -> x   
            return! messageLoop (new Dictionary<JobId option, Job>()) List.empty (getNewPersistDate ()) (getNewMultiplier waitMultiplier)                    
        }
        messageLoop (new Dictionary<JobId option, Job>()) List.empty (getNewPersistDate ()) 1
        )

    let enqueue (actor: MailboxProcessor<_>) (jobs: Job list) = 
        actor.Post (jobs, None)

    let enqueueWithCompletedAwaiter (actor: MailboxProcessor<_>) (jobs: Job list) =
        let (completedSignalFunc, awaiter) = CompletedSignalModule.createDefault ()
        actor.Post (jobs, Some completedSignalFunc)
        awaiter
    
    let createMongoJobStore (jobsCollection: IMongoCollection<MongoJob>) (workflowId: WorkflowId) = 
        let updateJobsActor = persistActor  (updateJobs jobsCollection)
        let enqueueJobsUpdate jobs = enqueueWithCompletedAwaiter updateJobsActor jobs
        let enqueueJobUpdate job = enqueueJobsUpdate [job]


        let addJobsActor = persistActor  (addJobs jobsCollection)
        let enqueueJobsAdd jobs = enqueue addJobsActor jobs
        let enqueueJobAdd job = enqueueJobsAdd [job]      

        { 
            getPendingJobs = GetPendingJobsFunc (getPendingJobs jobsCollection workflowId)
            getPendingResultHandlingJobs = GetPendingResultHandlingJobsFunc (getPendingResultHandlingJobs jobsCollection workflowId)
            getStaleJobs = GetStaleJobsFunc (getStaleJobs jobsCollection workflowId)
            getStaleResultHandlingJobs = GetStaleResultHandlingJobsFunc (getStaleResultHandlingJobs jobsCollection workflowId)
            addJobs = AddJobsFunc enqueueJobsAdd
            addJob = AddJobFunc enqueueJobAdd
            updateJobs = UpdateJobsFunc enqueueJobsUpdate
            updateJob = UpdateJobFunc enqueueJobUpdate
        }