// ask.fsx — module 07: ask the agent real questions, with the local model.
#load "agent.fsx"

open Agent

let questions =
    [ "Where is my order 4512?"
      "Where is my order 9999?" ]

for question in questions do
    printfn "=== %s" question

    match runAgent Llm.client question with
    | Ok text -> printfn "Answer: %s" text
    | Error error -> printfn "Stopped: %s" (describeError error)
