namespace FSharpSending.Results.Controllers

open System.Net.Http
open AgentsModule

type AgentsController() =
    member __.SendOrder (data: AgentsData) = async {
       return! SendOrder data
    }

    //... other controller methods for reflection invokation 
