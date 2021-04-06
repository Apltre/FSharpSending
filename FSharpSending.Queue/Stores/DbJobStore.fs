namespace FSharpSending.Queue.Stores

open FSharpSending.Common.Types.CommonTypes

type AddJobsFunc = AddJobsFunc of (Job list -> unit)
type AddJobFunc = AddJobFunc of (Job -> unit)
type UpdateJobsFunc = UpdateJobsFunc of (Job list -> unit)
type UpdateJobFunc = UpdateJobFunc of (Job -> unit)
type GetPendingJobsFunc = GetPendingJobsFunc of (unit -> Async<Job list>)
type GetPendingResultHandlingJobsFunc = GetPendingResultHandlingJobsFunc of (unit -> Async<Job list>)

type DbJobStore =
    { 
        getPendingJobs :  GetPendingJobsFunc
        getPendingResultHandlingJobs : GetPendingResultHandlingJobsFunc
        addJobs : AddJobsFunc
        addJob : AddJobFunc
        updateJobs : UpdateJobsFunc
        updateJob : UpdateJobFunc
    }