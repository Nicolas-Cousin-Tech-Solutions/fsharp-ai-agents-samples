// tool.fsx — module 05: a tool round trip, done by hand.
#load "llm.fsx" "orders.fsx"

open System
open System.Text.Json
open Microsoft.Extensions.AI
open Llm
open Orders

// 1. Describe the tool to the model: name, description, and parameter schema (inferred from getOrder).
let orderTool =
    AIFunctionFactory.Create(
        Func<int, string>(getOrder),
        "getOrder",
        "Returns the status of an order from its number."
    )

printfn "[schema sent to the model] %s" (orderTool.JsonSchema.GetRawText())

let options = createOptions ()
options.Tools <- ResizeArray<AITool> [ orderTool :> AITool ]

let history =
    ResizeArray<ChatMessage>
        [ ChatMessage(ChatRole.System, "You are the support assistant of an online shop. Use the tools to answer. Answer in English, briefly.")
          ChatMessage(ChatRole.User, "Where is my order 4512?") ]

// 2. First turn: the model answers… with a tool request, or with text.
let response1 = client.GetResponseAsync(history, options).GetAwaiter().GetResult()

let requests =
    response1.Messages
    |> Seq.collect (fun message -> message.Contents)
    |> Seq.choose (fun content ->
        match content with
        | :? FunctionCallContent as request -> Some request
        | _ -> None)
    |> List.ofSeq

printfn "[turn 1] tool requests: %d" requests.Length

if requests.IsEmpty then
    printfn "[turn 1] model text: %s" response1.Text
else
    // 3. Your code runs the tool, then adds the result to the history.
    history.AddRange(response1.Messages)

    for request in requests do
        printfn "[the model requests] %s %s" request.Name (JsonSerializer.Serialize request.Arguments)
        // Naive reading of the argument: module 06 will replace this cast with a real boundary.
        let number = (request.Arguments["orderId"] :?> JsonElement).GetInt32()
        let result = getOrder number
        printfn "[your code runs] getOrder %d -> %s" number result
        let content = FunctionResultContent(request.CallId, result) :> AIContent
        history.Add(ChatMessage(ChatRole.Tool, ResizeArray<AIContent> [ content ]))

    // 4. Second turn: the model reads the result and writes the final answer.
    let response2 = client.GetResponseAsync(history, options).GetAwaiter().GetResult()
    printfn "[turn 2] final answer: %s" response2.Text
