// agent.fsx — modules 07 and 08: bounded agent loop; each turn is a discriminated union.
// This file defines the agent without running it: ask.fsx and scenarios.fsx use it.
#load "llm.fsx" "orders.fsx" "decoding.fsx"

open System
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.AI
open Llm
open Orders
open Decoding

// What the model produced on this turn: one shape out of three.
type AgentStep =
    | FinalAnswer of text: string
    | CallTools of calls: (string * Result<ToolCall, ToolError>) list
    | InvalidResponse of reason: string

// Why the agent stopped without a final answer.
type AgentError =
    | TurnLimitReached of maxTurns: int
    | InvalidModelResponse of reason: string
    | ModelUnreachable of detail: string
    | ModelTimeout

let describeError (error: AgentError) : string =
    match error with
    | TurnLimitReached limit -> $"limit of {limit} turns reached"
    | InvalidModelResponse reason -> $"unusable model response ({reason})"
    | ModelUnreachable _ -> "model server unreachable"
    | ModelTimeout -> "model timeout exceeded"

let maxTurns = 4

let instructions =
    "You are the support assistant of an online shop. "
    + "Use the tools to answer. Answer in English, briefly."

let orderTool =
    AIFunctionFactory.Create(
        Func<int, string>(getOrder),
        "getOrder",
        "Returns the status of an order from its number."
    )

// Adapter: a .NET tool request -> the module 06 boundary.
let decodeRequest (request: FunctionCallContent) : Result<ToolCall, ToolError> =
    decode request.Name (JsonSerializer.Serialize request.Arguments)

let classify (response: ChatResponse) : AgentStep =
    let requests =
        response.Messages
        |> Seq.collect (fun message -> message.Contents)
        |> Seq.choose (fun content ->
            match content with
            | :? FunctionCallContent as request -> Some request
            | _ -> None)
        |> List.ofSeq

    if not requests.IsEmpty then
        CallTools [ for request in requests -> (request.CallId, decodeRequest request) ]
    elif String.IsNullOrWhiteSpace response.Text then
        InvalidResponse "empty response"
    else
        FinalAnswer(response.Text.Trim())

// Only ToolCall cases can be executed: that is the allowed list.
let execute (request: Result<ToolCall, ToolError>) : string =
    match request with
    | Ok(GetOrder number) -> getOrder number
    | Error error -> sprintf "Error: %A" error

// Module 08 — transport failures become named cases.
let callModel
    (client: IChatClient)
    (history: ResizeArray<ChatMessage>)
    (options: ChatOptions)
    =
    try
        Ok(client.GetResponseAsync(history, options).GetAwaiter().GetResult())
    with
    | :? HttpRequestException as ex -> Error(ModelUnreachable ex.Message)
    | :? TaskCanceledException -> Error ModelTimeout

let runAgent (client: IChatClient) (question: string) : Result<string, AgentError> =
    let options = createOptions ()
    options.Tools <- ResizeArray<AITool> [ orderTool :> AITool ]

    let history =
        ResizeArray<ChatMessage>
            [ ChatMessage(ChatRole.System, instructions)
              ChatMessage(ChatRole.User, question) ]

    let rec loop turn =
        if turn > maxTurns then
            Error(TurnLimitReached maxTurns)
        else
            match callModel client history options with
            | Error error -> Error error
            | Ok response ->
                match classify response with
                | FinalAnswer text ->
                    printfn "[turn %d] FinalAnswer" turn
                    Ok text
                | InvalidResponse reason ->
                    printfn "[turn %d] InvalidResponse %s" turn reason
                    Error(InvalidModelResponse reason)
                | CallTools requests ->
                    printfn "[turn %d] CallTools %A" turn (List.map snd requests)
                    history.AddRange(response.Messages)

                    for (callId, request) in requests do
                        let result = execute request
                        printfn "         -> %s" result
                        let content = FunctionResultContent(callId, result) :> AIContent
                        history.Add(ChatMessage(ChatRole.Tool, ResizeArray<AIContent> [ content ]))

                    loop (turn + 1)

    loop 1
