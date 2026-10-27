// mcp-server.fsx — module 09: expose getOrder to any MCP client.
// Tested on 2026-10-27 with ModelContextProtocol 2.3.0 and Microsoft.Extensions.Hosting 10.0.14.
#r "nuget: ModelContextProtocol, 2.3.0"
#r "nuget: Microsoft.Extensions.Hosting, 10.0.14"
#load "orders.fsx"

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server

// The F# function from module 04, unchanged, declared as an MCP tool.
let orderTool =
    McpServerTool.Create(
        Func<int, string>(Orders.getOrder),
        McpServerToolCreateOptions(
            Name = "getOrder",
            Description = "Returns the status of an order from its number."
        )
    )

// PROVIDED — the .NET generic host, as in a C# project.
let builder = Host.CreateApplicationBuilder()

// stdout is reserved for the protocol: all logs go to stderr.
builder.Logging.AddConsole(fun options -> options.LogToStandardErrorThreshold <- LogLevel.Trace)
|> ignore

builder.Services.AddMcpServer().WithStdioServerTransport().WithTools([ orderTool ])
|> ignore

builder.Build().RunAsync().GetAwaiter().GetResult()
