namespace FSharpSending.Sending.Controllers

open System.Net.Http
open AgentsModule

type AgentsController(client: HttpClient) =
    member __.SendOrders (data: AgentsData) = async {
       return! SendOrders client data
    }

    //... other controller methods for reflection invokation 
