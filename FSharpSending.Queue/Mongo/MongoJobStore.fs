namespace FSharpSending.Queue.Mongo

open MongoDB.Driver
open System
open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Queue.Stores

module MongoJobStore =
    let getPendingJobs (jobsCollection: IMongoCollection<MongoJob>) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> job.Status = JobStatus.Pending && job.StartTime <= DateTime.Now)
                                      .SortBy(fun job -> job.Id :> Object)
                                      .Limit(100)
                                      .ToListAsync() |> Async.AwaitTask
            return jobs  |> Seq.toList
                         |> List.map MongoJobModule.ofMongoJob
        }
          

    let getPendingResultHandlingJobs (jobsCollection: IMongoCollection<MongoJob>) ()  = 
        async {
            let! jobs = jobsCollection.Find<MongoJob>(fun job -> job.ResultHandlingStatus.Value = JobResultHandlingStatus.Pending 
                                                                 && job.ResultHandlingStartDate.Value <= DateTime.Now)
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
        let mapToList x =
            x |> Map.toList 
              |> List.map (fun (x, y) -> y)
        let rec messageLoop map persistDate waitMultiplier = async {
            let waitTime = minimalWaitMs * waitMultiplier
            let! msgOption = inbox.TryReceive(waitTime)
            match msgOption with
                | Some (job: Job) -> match (persistDate > DateTime.Now) && ((Map.count map) < maxBatchSize) with
                                     | true -> let newMap = Map.add job.Id job map
                                               return! messageLoop newMap persistDate 1
                                     | false -> let listToSave = map 
                                                                 |> Map.add job.Id job
                                                                 |> mapToList
                                                do! persist listToSave
                                                return! messageLoop Map.empty (getNewPersistDate ()) 1
                               
                | None -> match not map.IsEmpty with
                          | true -> do! persist (mapToList map)
                          | false -> ()
                          let getNewMultiplier x = 
                              match x < 5 with
                              | true -> x + 1
                              | false -> x   
                          return! messageLoop Map.empty (getNewPersistDate ()) (getNewMultiplier waitMultiplier)
                            
        }
        messageLoop Map.empty (getNewPersistDate ()) 1
        )

    let enqueue (actor: MailboxProcessor<_>) job = 
        actor.Post job
    
    let createMongoJobStore (jobsCollection: IMongoCollection<MongoJob>) = 
        let updateJobsActor = persistActor  (updateJobs jobsCollection)
        let enqueueJobUpdate = enqueue updateJobsActor
        let enqueueJobsUpdate jobs =
            jobs |> List.iter enqueueJobUpdate

        let addJobsActor = persistActor  (addJobs jobsCollection)
        let enqueueJobAdd = enqueue addJobsActor
        let enqueueJobsAdd jobs =
                   jobs |> List.iter enqueueJobAdd

        { 
            getPendingJobs = GetPendingJobsFunc (getPendingJobs jobsCollection)
            getPendingResultHandlingJobs = GetPendingResultHandlingJobsFunc (getPendingResultHandlingJobs jobsCollection)
            addJobs = AddJobsFunc enqueueJobsAdd
            addJob = AddJobFunc enqueueJobAdd
            updateJobs = UpdateJobsFunc enqueueJobsUpdate
            updateJob = UpdateJobFunc enqueueJobUpdate
        }