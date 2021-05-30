namespace FSharpSending.Common.Helpers.Json

open FSharpSending.Common.Types.CommonTypes
open Thoth.Json.Net

module JsonDecoder =
    let decode (message: string)  =
       // Decode.Auto.fromString<'t> (message , CaseStrategy.PascalCase)
       // |> Result.mapError DomainError.JsonSerializationFail
       Decode.fromString FSharpSending.Common.Types.JobConverter.ofJson message
       |> Result.mapError DomainError.JsonSerializationFail
