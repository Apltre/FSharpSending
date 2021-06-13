module AgentsModule

open System
open ResultsTypes
open System.Net

    type AgentsData = {
        Url: string
    }

    type ErrorMessage = {
        Code: int
    }

    let SendOrder_HandleSuccessAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

    let SendOrder_HandleLogicalErrorAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok ({ Code = -1 } :> Object)
    }

    let SendOrder_HandleFatalErrorAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

    let SendOrder_HandleTemporaryError (data: AgentsData) (error: ErrorMessage) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

