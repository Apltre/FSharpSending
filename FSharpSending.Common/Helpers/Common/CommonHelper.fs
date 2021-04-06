module CommonHelper

open System
open FSharpSending.Common.Types.CommonTypes

let getStartDelay (AttemptNumber attemptIndex) =
    DateTime.Now.AddMinutes(5.0 * (float attemptIndex))