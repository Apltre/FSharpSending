module PipeAsync

let switch f a = async {
    return f a
}

let tee f aAsync = async {
    let! a' = aAsync
    do! f a'
    return a'
}

let teeSync f aAsync = async {
    let! a' = aAsync
    f a'
    return a'
}

let mapUnit aAsync = async {
    let! res = aAsync
    return ()
}