namespace FSharpSending.Queue.Stores

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Common.Helpers.Signal

module JobMessageBus =
    type ToResultBusQueueFunc = ToResultBusQueueFunc of (Job -> unit)
    type ToSendingBusQueueFunc = ToSendingBusQueueFunc of (Job -> unit)
    type GetResultsBusConsumerFunc = GetResultsBusConsumerFunc of ((string -> Async<Result<unit, Errors>>) -> unit)
    type GetSendingBusConsumerFunc = GetSendingBusConsumerFunc of ((string -> Async<Result<unit, Errors>>) -> unit)

    type MessageBusStore = {
         enqueueToResult: ToResultBusQueueFunc
         enqueueToSending: ToSendingBusQueueFunc
         getResultsConsumer: GetResultsBusConsumerFunc
         getSendingConsumer: GetSendingBusConsumerFunc
         initializeQueues: (unit -> unit)
     }

module DbJob =
    type AddJobsFunc = AddJobsFunc of (Job list -> unit)
    type AddJobFunc = AddJobFunc of (Job -> unit)
    type UpdateJobsFunc = UpdateJobsFunc of (Job list -> CompletedSignalAwaiter)
    type UpdateJobFunc = UpdateJobFunc of (Job -> CompletedSignalAwaiter)
    type GetPendingJobsFunc = GetPendingJobsFunc of (unit -> Async<Job list>)
    type GetPendingResultHandlingJobsFunc = GetPendingResultHandlingJobsFunc of (unit -> Async<Job list>)
    type GetStaleJobsFunc = GetStaleJobsFunc of (unit -> Async<Job list>)
    type GetStaleResultHandlingJobsFunc = GetStaleResultHandlingJobsFunc of (unit -> Async<Job list>)

    type DbJobStore =
        { 
            getPendingJobs :  GetPendingJobsFunc
            getPendingResultHandlingJobs : GetPendingResultHandlingJobsFunc
            getStaleJobs :  GetStaleJobsFunc
            getStaleResultHandlingJobs : GetStaleResultHandlingJobsFunc
            addJobs : AddJobsFunc
            addJob : AddJobFunc
            updateJobs : UpdateJobsFunc
            updateJob : UpdateJobFunc
        }