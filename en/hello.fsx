// hello.fsx — module 02: a first question to the local model.
#load "llm.fsx"

open System.Diagnostics
open Microsoft.Extensions.AI
open Llm

checkOllama ()

let question = "In one sentence, what is an AI agent?"

let stopwatch = Stopwatch.StartNew()
let response = client.GetResponseAsync(question, createOptions ()).GetAwaiter().GetResult()

printfn "Model   : %s" model
printfn "Answer  : %s" response.Text
printfn "Duration: %.1f s" stopwatch.Elapsed.TotalSeconds
