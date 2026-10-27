// serveur-mcp.fsx — module 09 : exposer getOrder à n'importe quel client MCP.
// Testé le 2026-10-27 : ModelContextProtocol 2.3.0, Microsoft.Extensions.Hosting 10.0.14.
#r "nuget: ModelContextProtocol, 2.3.0"
#r "nuget: Microsoft.Extensions.Hosting, 10.0.14"
#load "commandes.fsx"

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server

// La fonction F# du module 04, inchangée, déclarée comme outil MCP.
let outilCommande =
    McpServerTool.Create(
        Func<int, string>(Commandes.getOrder),
        McpServerToolCreateOptions(
            Name = "getOrder",
            Description = "Retourne le statut d'une commande à partir de son numéro."
        )
    )

// FOURNI — l'hôte générique .NET, comme dans un projet C#.
let builder = Host.CreateApplicationBuilder()

// stdout est réservé au protocole : tous les journaux partent sur stderr.
builder.Logging.AddConsole(fun options ->
    options.LogToStandardErrorThreshold <- LogLevel.Trace)
|> ignore

builder.Services.AddMcpServer().WithStdioServerTransport().WithTools([ outilCommande ])
|> ignore

builder.Build().RunAsync().GetAwaiter().GetResult()
