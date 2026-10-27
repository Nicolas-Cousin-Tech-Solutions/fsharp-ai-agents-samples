// naive.fsx — module 06: deserializing the model's arguments directly.
open System.Text.Json

type OrderArguments = { orderId: int }

let attempt (argsJson: string) =
    try
        let value = JsonSerializer.Deserialize<OrderArguments> argsJson
        printfn "%-20s -> %A" argsJson value
    with ex ->
        printfn "%-20s -> exception %s" argsJson (ex.GetType().Name)

attempt """{"orderId":4512}"""
attempt """{}"""
attempt """{"orderId":"ABC-12"}"""
attempt """{"orderId":45"""
