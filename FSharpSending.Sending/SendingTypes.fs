module SendingTypes

open System

type SendingError =
    | CriticalFail of Object
    | LogicalFail of Object
    | TemporaryFail of Object

