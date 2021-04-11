module Pipe

let tee f x =
    f x; x

