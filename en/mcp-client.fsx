// mcp-client.fsx — module 09, PROVIDED: an MCP client for verification.
// It starts mcp-server.fsx, lists its tools, then calls getOrder three times.
#r "nuget: ModelContextProtocol, 2.3.0"

open System
open System.Collections.Generic
open ModelContextProtocol.Client
open ModelContextProtocol.Protocol

let serverPath = IO.Path.Combine(__SOURCE_DIRECTORY__, "mcp-server.fsx")

let transportOptions =
    StdioClientTransportOptions(
        Name = "orders",
        Command = "dotnet",
        Arguments = [| "fsi"; serverPath |]
    )

let transport = StdioClientTransport(transportOptions)

let client = McpClient.CreateAsync(transport).GetAwaiter().GetResult()

for tool in client.ListToolsAsync().AsTask().GetAwaiter().GetResult() do
    printfn "Tool: %s — %s" tool.Name tool.Description
    printfn "Schema: %s" (tool.JsonSchema.GetRawText())

let call (argument: obj) =
    let arguments = Dictionary<string, obj>(dict [ ("orderId", argument) ])
    let pending = client.CallToolAsync("getOrder", arguments)
    let result = pending.AsTask().GetAwaiter().GetResult()

    let text =
        result.Content
        |> Seq.choose (fun block ->
            match block with
            | :? TextContentBlock as t -> Some t.Text
            | _ -> None)
        |> String.concat ""

    let isError = (result.IsError = Nullable true)
    printfn "getOrder(%A) -> IsError=%b : %s" argument isError text

call (box 4512)
call (box 9999)
call (box "abc")

client.DisposeAsync().AsTask().GetAwaiter().GetResult()
