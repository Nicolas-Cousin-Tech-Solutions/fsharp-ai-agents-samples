// samples.fsx — module 06: raw tool requests, as a model would send them.
#load "decoding.fsx"

open Decoding

let samples =
    [
        ("getOrder", """{"orderId":4512}""")
        ("cancelOrder", """{"orderId":7781}""")
        ("getOrder", """{}""")
        ("getOrder", """{"orderId":"ABC-12"}""")
        ("getOrder", """{"orderId":45""")
    ]

for (name, argsJson) in samples do
    printfn "%-11s %-20s -> %A" name argsJson (decode name argsJson)
