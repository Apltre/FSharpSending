module JobProcessor

open FSharpSending.Common.Types.CommonTypes
open FSharpSending.Sending.Stores.JobMessageBus
open System
open System.Reflection
open System.Linq
open Microsoft.Extensions.DependencyInjection
open System.Text.Json
open System.Threading.Tasks

    type Operation = {
        Method: MethodInfo
        ControllerType: Type
        ArgType: Type
    }

    let getOperation (sendingType: SendingType) =
        let handlerName = sendingType.ToString().Split("_")
        let controllerFullName = $"FSharpSending.Sending.Controllers.{handlerName.[0]}Controller"
        let controllerType = Assembly.GetExecutingAssembly().GetType(controllerFullName)
        let methodName = handlerName.[1]
        let asyncName name = 
            $"{name}Async"
        let getMethod name =
            let getSyncMethod name =
                controllerType.GetMethod name |> Option.ofObj
            let getAsyncMethod name =
                controllerType.GetMethod (asyncName name) |> Option.ofObj
            getSyncMethod name
            |> Option.orElseWith (fun () -> getAsyncMethod (asyncName name))

        match controllerType with
        | null -> Result.Error (DomainError.Error $"cannot find controller: {controllerFullName}")
        | _ -> let method = getMethod methodName
               match method  with
               | None -> Result.Error (DomainError.Error $"Не найден ни один из методов ('{(asyncName methodName)}', '{methodName}')")
               | Some methodInfo -> 
                    Ok ({
                            Method = methodInfo
                            ControllerType = controllerType
                            ArgType = methodInfo.GetParameters().Single().ParameterType
                        })

    let processJob (insertInQueueQueue: ToQueueBusQueueFunc) (serviceProvider: IServiceProvider) (job: Job) : Async<Result<_, DomainError>> = async {
        let operationResult = getOperation job.SendingInfo.Type
        let getArg (argType: Type) (jobData: string option) =
            match jobData with
            | None -> null
            | Some jobData' ->
                match argType <> typeof<string> with
                | false -> job.SendingInfo.Data :> Object
                | true ->  (JsonSerializer.Deserialize jobData' argType) :> Object

        match operationResult with
        | Result.Error err -> return Result.Error err
        | Result.Ok operation ->
            use scope = serviceProvider.CreateScope()
            let controller = scope.ServiceProvider.GetRequiredService operation.ControllerType
            let args = [| getArg operation.ArgType job.SendingInfo.Data|]
            let! result  = operation.Method.Invoke (controller, args) :?> Task<Object>
                           |> Async.AwaitTask
            result (Result.Ok result)
    }