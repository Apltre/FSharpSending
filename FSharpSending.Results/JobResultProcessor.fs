module JobResultProcessor

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Results.Stores.JobMessageBus
open System
open System.Reflection
open System.Linq
open ResultsTypes
open Newtonsoft.Json
open Microsoft.Extensions.DependencyInjection

    type Operation = {
        Method: MethodInfo
        ControllerType: Type
        ArgsTypes: Type list
    }

    let getOperation (sendingType: SendingType) (jobStatus: JobStatus) =
        let handlerName = sendingType.ToString().Split("_")
        let controllerFullName = $"FSharpSending.Results.Controllers.{handlerName.[0]}Controller"
        let controllerType = Assembly.GetExecutingAssembly().GetType(controllerFullName)
        let methodName = handlerName.[1]

        let getControllerMethodName baseName jobStatus =
            match jobStatus with
            | JobStatus.FinishedSuccessfully -> Ok $"{baseName}_HandleSuccess"
            | JobStatus.ResendableError -> Ok $"{baseName}_HandleTemporaryError"
            | JobStatus.UnresendableError -> Ok $"{baseName}_HandleLogicalError"
            | JobStatus.FatalError -> Ok $"{baseName}_HandleFatalError"
            | _ -> Result.Error (Errors.Error "Unexpexted status")

        let getMethod name =
            let asyncName name = 
                $"{name}Async"
            let getSyncMethod name =
                controllerType.GetMethod name |> Option.ofObj
            let getAsyncMethod name =
                controllerType.GetMethod (asyncName name) |> Option.ofObj
            let method = getSyncMethod name
                         |> Option.orElseWith (fun () -> getAsyncMethod (asyncName name))
            match method with
            | None -> Result.Error (Errors.Error $"No method with name {name} found")
            | Some method -> Ok method

        match controllerType with
        | null -> Result.Error (Errors.Error $"cannot find controller: {controllerFullName}")
        | _ -> let method = getControllerMethodName methodName jobStatus
                            |> Result.bind getMethod
               match method with
               | Result.Error err -> Result.Error err
               | Ok methodInfo -> 
                    Ok ({
                            Method = methodInfo
                            ControllerType = controllerType
                            ArgsTypes = methodInfo.GetParameters()
                                      |> Seq.map (fun paramInfo -> paramInfo.ParameterType)
                                      |> Seq.toList
                        })

    let runAsync (serviceProvider: IServiceProvider) (job: Job) : Async<Result<Object, ResultsError>> = async {
        let operationResult = getOperation job.SendingInfo.Type job.SendingInfo.Status
        let getArgFromJson (argType: Type) (json: string option) =
            match json with
            | None -> null
            | Some json' ->
                match argType <> typeof<string> with
                | false -> job.SendingInfo.Data :> Object
                | true ->  JsonConvert.DeserializeObject (json', argType)

        let fillArgs (methodArgsTypes: Type list) json (args: Object list) =
            match List.length methodArgsTypes with
            | 1 -> Ok args
            | 2 -> let secondArg = getArgFromJson methodArgsTypes.[1] json
                   Ok (List.rev (secondArg :: args))
            | _ -> Result.Error (Errors.Error $"Unexpected method args number.")
            

        match operationResult with
        | Result.Error err -> return Result.Error (ResultsError.CriticalFail (err :> Object))
        | Result.Ok operation ->
            use scope = serviceProvider.CreateScope()
            let controller = scope.ServiceProvider.GetRequiredService operation.ControllerType
            let argsResult = [ getArgFromJson operation.ArgsTypes.Head job.SendingInfo.Data ]
                             |> fillArgs operation.ArgsTypes job.SendingInfo.Message
                             |> Result.map List.toArray
            match argsResult with
            | Ok args -> return! operation.Method.Invoke (controller, args) :?> Async<Result<Object, ResultsError>>
            | Result.Error err -> return (Result.Error err)
    }

    let processJob (ToQueueBusQueueFunc insertInQueueQueue) (serviceProvider: IServiceProvider) (job: Job) : Async<ResuErrorsmainError>> = async {
        try
            let! result = runAsync serviceProvider job
            let changeJobStatus job jobResultStatus message  =
                 let message' = match message with
                                | null -> None
                                | _ -> Some (JsonConvert.SerializeObject message)
                 { job with ResultHandlingInfo = { job.ResultHandlingInfo with ResultHandlingStatus = Some jobResultStatus
                                                                               ResultHandlingMessage = message'
                                                                               ResultHandlingProcessedDate = Some DateTime.Now
                 }}

            let changeAndEnqueue jobResultStatus message =
                changeJobStatus job jobResultStatus message |> insertInQueueQueue

            match result with
            | Ok x -> return Ok (changeAndEnqueue JobResultHandlingStatus.FinishedSuccessfully x)
            | Result.Error err -> 
                match err with
                | LogicalFail lf -> 
                    return Ok (changeAndEnqueue JobResultHandlingStatus.Failed lf)
                | CriticalFail cf -> 
                    return Ok (changeAndEnqueue JobResultHandlingStatus.FatalError cf)
                | TemporaryFail tf ->
                    return Ok (changeErrorsJobResultHandlingStatus.Pending tf)
        with 
        | ex -> return Result.Error (DomainError.ErrorExn ex)
    }