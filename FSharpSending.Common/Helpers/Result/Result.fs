module Result
// apply either a success function or failure function
let either successFunc failureFunc twoTrackInput =
    match twoTrackInput with
    | Ok s -> successFunc s
    | Error f -> failureFunc f

// pipe a two-track value into a switch function
let (>>=) x f =
    Result.bind f x

// compose two switches into another switch
let (>=>) s1 s2 =
    s1 >> Result.bind s2

// convert a one-track function into a switch
let switch f =
    f >> Ok

// convert a dead-end function into a one-track function

 // apply function to success ignoring it's result and passing two track input further 
let teeOk f twoTrackInput =
    either ((Pipe.tee f) >> Ok)  Error twoTrackInput

// apply function to error ignoring it's result and passing two track input further 
let teeError f twoTrackInput =
   either Ok ((Pipe.tee f) >> Error) twoTrackInput

// convert a one-track function into a switch with exception handling
let tryCatch f exnHandler x =
    try
        f x |> Ok
    with
    | ex -> exnHandler ex |> Error

// convert two one-track functions into a two-track function
let doubleMap successFunc failureFunc =
    either (successFunc >> Ok) (failureFunc >> Error)

// add two switches in parallel
let plus addSuccess addFailure switch1 switch2 x =
    match (switch1 x),(switch2 x) with
    | Ok s1,Ok s2 -> Ok (addSuccess s1 s2)
    | Error f1,Ok _  -> Error f1
    | Ok _ ,Error f2 -> Error f2
    | Error f1,Error f2 -> Error (addFailure f1 f2)