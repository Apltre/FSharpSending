module ResultAsync

let bindToAsync f twoTrackInput = async {
    match twoTrackInput with
    | Error err -> return Error err
    | Ok x -> return! f x
}

let teeToAsync f twoTrackInput = async {
    match twoTrackInput with
    | Error err -> return (Error err)
    | Ok x -> do! f x
              return twoTrackInput
}

let tee f twoTrackInputAsync = async {
    let! twoTrackInput = twoTrackInputAsync
    match twoTrackInput with
    | Error err -> return (Error err)
    | Ok x -> do! f x
              return twoTrackInput
}

let teeSync f twoTrackInputAsync = async {
    let! twoTrackInput = twoTrackInputAsync
    match twoTrackInput with
    | Error err -> return (Error err)
    | Ok x -> f x
              return twoTrackInput
}

let map f twoTrackInputAsync = async {
    let! twoTrackInput = twoTrackInputAsync
    match twoTrackInput with
       | Error err -> return (Error err)
       | Ok x -> let! result = f x
                 return Ok result
}

let mapUnit twoTrackInputAsync = async {
    return! map (fun ok -> async { return () }) twoTrackInputAsync
}