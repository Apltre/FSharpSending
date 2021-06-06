namespace FSharpSending.Common.Helpers.Json

open FSharpSending.Common.Types.CommonTypes
open Thoth.Json.Net

module JsonDecoder =
    let decode (message: string)  =
       Decode.fromString FSharpSending.Common.Types.JobConverter.ofJson message
       |> Result.mapError Errors.JsonSerializationFail
