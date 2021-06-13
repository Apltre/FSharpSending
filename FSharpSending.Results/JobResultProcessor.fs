module JobResultProcessor

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Results.Stores.JobMessageBus
open System
open System.Reflection
open ResultsTypes
open Newtonsoft.Json
open Microsoft.Extensions.DependencyInjection

    type Operation = {
        Method: MethodInfo
        ControllerType: Type
        ArgsTypes: Type list
    }

    let getMethod (controllerType: Type) name  =
        let asyncName name = 
            $"{name}Async"
        let getSyncMethod name =
            controllerType.GetMethod name |> Option.ofObj
        let getAsyncMethod name =
            controllerType.GetMethod (asyncName name) |> Option.ofObj
        getSyncMethod name
        |> Option.orElseWith (fun () -> getAsyncMethod (asyncName name))

    let getOperation (sendingType: SendingType) (jobStatus: JobStatus) =
        let handlerName = sendingType.ToString().Split("_")
        let controllerFullName = $"FSharpSending.Results.Controllers.{handlerName.[0]}Controller"
        let controllerType = Assembly.GetExecutingAssembly().GetType(controllerFullName)
        let methodName = handlerName.[1]

        let getControllerMethodName baseName jobStatus =
            match jobStatus with
            | JobStatus.FinishedSuccessfully -> Some $"{baseName}_HandleSuccess"
            | JobStatus.ResendableError -> Some $"{baseName}_HandleTemporaryError"
            | JobStatus.UnresendableError -> Some $"{baseName}_HandleLogicalError"
            | JobStatus.FatalError -> Some $"{baseName}_HandleFatalError"
            | _ -> None

        match controllerType with
        | null -> Result.Error (Errors.Error $"cannot find controller: {controllerFullName}")
        | _ -> let methodInfoOpt = getControllerMethodName methodName jobStatus
                                   |> Option.bind (getMethod controllerType)
               match methodInfoOpt with
               | None -> Ok None
               | Some methodInfo -> 
                    Ok (Some {
                                 Method = methodInfo
                                 ControllerType = controllerType
                                 ArgsTypes = methodInfo.GetParameters()
                                           |> Seq.map (fun paramInfo -> paramInfo.ParameterType)
                                           |> Seq.toList
                             })

    let getArgFromJson (argType: Type) (json: string option) =
              match json with
              | None -> null
              | Some json' ->
                  match argType <> typeof<string> with
                  | false -> json :> Object
                  | true ->  JsonConvert.DeserializeObject (json', argType)

    let fillArgs (methodArgsTypes: Type list) json (args: Object list) =
        match List.length methodArgsTypes with
        | 1 -> Ok args
        | 2 ->  let secondArg = getArgFromJson methodArgsTypes.[1] json
                Ok (List.rev (secondArg :: args))
        | _ -> Result.Error "Unexpected method args number."

    let runAsync (serviceProvider: IServiceProvider) (job: Job) : Async<Result<Object, ResultsError>> = async {
        let operationResult = getOperation job.SendingInfo.Type job.SendingInfo.Status          
        match operationResult with
        | Result.Error err -> return Result.Error (ResultsError.CriticalFail err)
        | Result.Ok operationOption ->
            match  operationOption with
            | None -> return (Ok null)
            | Some operation ->
                use scope = serviceProvider.CreateScope()
                let controller = scope.ServiceProvider.GetRequiredService operation.ControllerType
                let argsResult = [ getArgFromJson operation.ArgsTypes.Head job.SendingInfo.Data ]
                                 |> fillArgs operation.ArgsTypes job.SendingInfo.Message
                                 |> Result.map List.toArray
                match argsResult with
                | Ok args -> return! operation.Method.Invoke (controller, args) :?> Async<Result<Object, ResultsError>>
                | Result.Error err -> return Result.Error (ResultsError.CriticalFail err)
    }

    let changeJobStatus job jobResultStatus message  =
         let message' = match message with
                        | null -> None
                        | _ -> Some (JsonConvert.SerializeObject message)
         { job with ResultHandlingInfo = { job.ResultHandlingInfo with ResultHandlingStatus = Some jobResultStatus
                                                                       ResultHandlingMessage = message'
                                                                       ResultHandlingProcessedDate = Some DateTime.Now
         }}

    let processJob (ToQueueBusQueueFunc insertInQueueQueue) (serviceProvider: IServiceProvider) (job: Job) : Async<Result<unit, Errors>> = async {
        try
            let! result = runAsync serviceProvider job
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
                    return Ok (changeAndEnqueue JobResultHandlingStatus.Pending tf)
        with 
        | ex -> return Result.Error (Errors.ErrorExn ex)
    }