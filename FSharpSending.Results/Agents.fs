module AgentsModule

open System
open ResultsTypes

    type AgentsData = {
        Url: string
    }

    let SendOrder_HandleSuccessAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

    let SendOrder_HandleLogicalErrorAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

    let SendOrder_HandleFatalErrorAsync (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

    let SendOrder_HandleTemporaryError (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
        return Ok null
    }

