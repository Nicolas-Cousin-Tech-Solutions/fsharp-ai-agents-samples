// decoding.fsx — module 06: the boundary between the model's data and your code.
// No package: System.Text.Json is part of .NET.
// This file prints nothing: it is loaded by samples.fsx and agent.fsx.

open System.Text.Json

// What your code agrees to execute: the closed list of allowed tools.
type ToolCall =
    | GetOrder of orderId: int

// Everything that can be wrong in a tool request, with a name.
type ToolError =
    | UnknownTool of name: string
    | InvalidJson of raw: string
    | MissingArgument of name: string
    | BadArgumentType of name: string * received: string

// Step 1 — is the tool name on the allowed list?
let checkToolName (name: string) : Result<string, ToolError> =
    if name = "getOrder" then Ok name else Error(UnknownTool name)

// Step 2 — is the arguments text a JSON object?
let readJson (argsJson: string) : Result<JsonElement, ToolError> =
    try
        use document = JsonDocument.Parse argsJson
        if document.RootElement.ValueKind = JsonValueKind.Object then
            Ok(document.RootElement.Clone())
        else
            Error(InvalidJson argsJson)
    with :? JsonException ->
        Error(InvalidJson argsJson)

// Step 3 — is the orderId argument present, and is it an integer?
let readOrderId (root: JsonElement) : Result<int, ToolError> =
    match root.TryGetProperty "orderId" with
    | false, _ -> Error(MissingArgument "orderId")
    | true, value ->
        let isInteger =
            value.ValueKind = JsonValueKind.Number && fst (value.TryGetInt32())
        if isInteger then
            Ok(value.GetInt32())
        else
            Error(BadArgumentType("orderId", value.ToString()))

// The pipeline: the first error short-circuits the following steps.
let decode (name: string) (argsJson: string) : Result<ToolCall, ToolError> =
    checkToolName name
    |> Result.bind (fun _ -> readJson argsJson)
    |> Result.bind readOrderId
    |> Result.map GetOrder
