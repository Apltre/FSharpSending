namespace FSharpSending.Results.Controllers
open AgentsModule
open System.Net.Http

type AgentsController(notificationsClient : HttpClient) =
    member __.SendOrder_HandleSuccessAsync (data: AgentsData) = async {
        return! SendOrder_HandleSuccessAsync data
    }

    member __.SendOrder_HandleLogicalErrorAsync (data: AgentsData) = async {
        return! SendOrder_HandleLogicalErrorAsync data
    }

    member __.SendOrder_HandleFatalErrorAsync (data: AgentsData) = async {
        return! SendOrder_HandleFatalErrorAsync data
    }

    member __.SendOrder_HandleTemporaryError (data: AgentsData) (error: ErrorMessage) = async {
        return! SendOrder_HandleTemporaryError data error
    }
