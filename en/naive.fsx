// naive.fsx — module 06: what happens when you deserialize the model's arguments directly.
open System.Text.Json

type OrderArguments = { orderId: int }

let attempt (argsJson: string) =
    try
        printfn "%-20s -> %A" argsJson (JsonSerializer.Deserialize<OrderArguments> argsJson)
    with ex ->
        printfn "%-20s -> exception %s" argsJson (ex.GetType().Name)

attempt """{"orderId":4512}"""
attempt """{}"""
attempt """{"orderId":"ABC-12"}"""
attempt """{"orderId":45"""
