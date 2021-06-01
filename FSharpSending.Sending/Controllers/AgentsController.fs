namespace FSharpSending.Sending.Controllers

open System.Net.Http
open AgentsModule

type AgentsController(client: HttpClient) =
    member __.SendOrder (data: AgentsData) = async {
       return! SendOrder client data
    }

    //... other controller methods for reflection invokation 
