module Logger

open Microsoft.Extensions.Logging
open FSharpSending.Common.Types.CommonTypes

type LogErrorFunc = LogErrorFunc of (Errors -> unit)
type LogInfoFunc = LogInfoFunc of (string -> unit)

type LoggerStore = 
    {
        logError : LogErrorFunc
        logMessage : LogInfoFunc
    }

let logError (logger : ILogger) (error : Errors)  =
    match error with
    | Error e -> logger.LogError e
    | ErrorExn exn -> logger.LogError (exn, "")
    | JsonSerializationFail e -> logger.LogError e
    | DbUpdateFailExn exn -> logger.LogError(exn, "DbUpdateFail!")
    | DbQueryFailExn exn -> logger.LogError(exn, "DbQueryFail!")
    | MessageQueueFailExn exn -> logger.LogError(exn, "MessageQueueFail!")
    | MessageQueueEnqueueFailExn exn -> logger.LogError(exn, "MessageQueueEnqueueFail")
    | MessageQueueConsumeFailExn exn -> logger.LogError(exn, "MessageQueueConsumeFail")
    | MessageQueueConsumeFail e -> logger.LogError e

let logMessage (logger : ILogger) message  =
   logger.LogInformation message
    
let createLogger (logger : ILogger) =
    {
        logError = LogErrorFunc (logError logger)
        logMessage = LogInfoFunc (logMessage logger)
    }
 