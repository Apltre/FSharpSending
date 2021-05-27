module AgentsModule

open System.Net.Http
open System
open SendingTypes

    type AgentsData = {
        message: string
    }

    let SendOrders (client: HttpClient) (data: AgentsData) : Async<Result<Object, SendingError>> = async {
         // do smth
         return Result.Ok(null)
    }

