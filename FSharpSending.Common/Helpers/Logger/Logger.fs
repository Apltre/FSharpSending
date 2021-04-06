module Logger

open Microsoft.Extensions.Logging
open FSharpSending.Common.Types.CommonTypes

type LogErrorFunc = LogErrorFunc of (DomainError -> unit)
type LogInfoFunc = LogInfoFunc of (string -> unit)

type LoggerStore = 
    {
        logError : LogErrorFunc
        logMessage : LogInfoFunc
    }

let logError (logger : ILogger) (error : DomainError)  =
    match error with
    | JsonSerializationFail e -> logger.LogError e
    | DbQueryFailExn exn -> logger.LogError(exn, "DbQueryFail!")
    | DbUpdateFailExn exn -> logger.LogError(exn, "DbUpdateFail!")
    | Error e -> logger.LogError(e)
    | MessageQueueFailExn exn -> logger.LogError(exn, "MessageQueueFail")

let logMessage (logger : ILogger) message  =
   logger.LogInformation message
    
let createLogger (logger : ILogger) =
    {
        logError = LogErrorFunc (logError logger)
        logMessage = LogInfoFunc (logMessage logger)
    }
 