module AgentsModule

open System.Net.Http
open FSharpSending.Common.Types.CommonTypes
    type AgentsData = {
        message: string
    }

    let SendOrders (client: HttpClient) (data: AgentsData) : Async<Result<unit, string>> = async {
         return (Result.Error "")
    }

