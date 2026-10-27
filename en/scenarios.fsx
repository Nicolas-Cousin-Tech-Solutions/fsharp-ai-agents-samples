// scenarios.fsx — module 08: replay failures, reproducibly.
#load "agent.fsx"

open System
open System.Collections.Generic
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.AI
open Agent

// ---------------------------------------------------------------------------
// PROVIDED — a fake IChatClient that replays responses written in advance.
// This is not a testing lesson: it is a "scenario player" that reliably produces
// responses the real model does not produce on demand.
let fakeClient (responses: ChatResponse list) : IChatClient =
    let queue = Queue<ChatResponse>(responses)

    { new IChatClient with
        member _.GetResponseAsync(_, _, _) = Task.FromResult(queue.Dequeue())
        member _.GetStreamingResponseAsync(_, _, _) = raise (NotSupportedException())
        member _.GetService(_, _) = null
        member _.Dispose() = () }

// A "model" response that requests a tool (arguments as JsonElement, as with Ollama).
let toolRequest (name: string) (argsJson: string) : ChatResponse =
    let args = JsonSerializer.Deserialize<Dictionary<string, obj>>(argsJson)
    let request = FunctionCallContent("call-1", name, args) :> AIContent
    ChatResponse(ChatMessage(ChatRole.Assistant, ResizeArray<AIContent> [ request ]))

let text (content: string) : ChatResponse =
    ChatResponse(ChatMessage(ChatRole.Assistant, content))
// ---------------------------------------------------------------------------

let printOutcome (outcome: Result<string, AgentError>) =
    match outcome with
    | Ok answer -> printfn "Outcome: Ok \"%s\"" answer
    | Error error -> printfn "Outcome: Error — %s" (describeError error)

let scenario (title: string) (client: IChatClient) =
    printfn "=== %s" title
    printOutcome (runAgent client "Where is my order 7781?")

scenario
    "1. Tool not on the allowed list"
    (fakeClient [ toolRequest "cancelOrder" """{"orderId":7781}"""
                  text "I cannot cancel an order." ])

scenario
    "2. Wrongly typed argument"
    (fakeClient [ toolRequest "getOrder" """{"orderId":"ABC-12"}"""
                  text "Could you give me an order number?" ])

scenario
    "3. The model asks for the tool again and again"
    (fakeClient (List.replicate 10 (toolRequest "getOrder" """{"orderId":7781}""")))

scenario
    "4. Model server stopped"
    (Llm.createClient "http://localhost:1" (TimeSpan.FromMinutes 3.0))

scenario
    "5. Timeout too short"
    (Llm.createClient Llm.address (TimeSpan.FromMilliseconds 1.0))
