namespace FSharpSending.Common.Helpers.Json

open FSharpSending.Common.Types.CommonTypes
open Thoth.Json.Net

module JsonDecoder =
    let decode (message: string)  =
        Decode.Auto.fromString<'t> (message , CaseStrategy.SnakeCase)
        |> Result.mapError DomainError.JsonSerializationFail
