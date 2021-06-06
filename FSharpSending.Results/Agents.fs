module AgentsModule

open System.Net.Http
open System.Net
open System
open ResultsTypes
open Newtonsoft.Json

    type AgentsData = {
        Url: string
    }

    let getResponseObj (response : HttpResponseMessage) = async {
        let! contentJson = response.Content.ReadAsStringAsync () |> Async.AwaitTask
        return JsonConvert.DeserializeObject contentJson
    }
    let SendOrder (client: HttpClient) (data: AgentsData) : Async<Result<Object, ResultsError>> = async {
         let! response = client.GetAsync(data.Url) |> Async.AwaitTask
 
         let! responseObj = getResponseObj response
         let result' = match response.StatusCode with
                       | HttpStatusCode.OK -> Result.Ok ({| StatusCode = HttpStatusCode.OK |} :> Object)
                       | HttpStatusCode.BadRequest -> Result.Error (ResultsError.TemporaryFail responseObj)
                       | _ -> Result.Error (ResultsError.LogicalFail responseObj)
         return result'
    }

