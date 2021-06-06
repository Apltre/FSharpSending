module ResultsTypes

open System

type ResultsError =
    | CriticalFail of Object
    | LogicalFail of Object
    | TemporaryFail of Object

