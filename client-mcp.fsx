// client-mcp.fsx — module 09, FOURNI : un client MCP de vérification.
// Il démarre serveur-mcp.fsx, liste ses outils, puis appelle getOrder trois fois.
#r "nuget: ModelContextProtocol, 2.3.0"

open System
open System.Collections.Generic
open ModelContextProtocol.Client
open ModelContextProtocol.Protocol

let cheminServeur = IO.Path.Combine(__SOURCE_DIRECTORY__, "serveur-mcp.fsx")

let transport =
    StdioClientTransport(
        StdioClientTransportOptions(Name = "commandes", Command = "dotnet", Arguments = [| "fsi"; cheminServeur |])
    )

let client = McpClient.CreateAsync(transport).GetAwaiter().GetResult()

for outil in client.ListToolsAsync().AsTask().GetAwaiter().GetResult() do
    printfn "Outil : %s — %s" outil.Name outil.Description
    printfn "Schéma : %s" (outil.JsonSchema.GetRawText())

let appeler (argument: obj) =
    let arguments = Dictionary<string, obj>(dict [ ("orderId", argument) ])
    let resultat = client.CallToolAsync("getOrder", arguments).AsTask().GetAwaiter().GetResult()

    let texte =
        resultat.Content
        |> Seq.choose (fun bloc ->
            match bloc with
            | :? TextContentBlock as t -> Some t.Text
            | _ -> None)
        |> String.concat ""

    printfn "getOrder(%A) -> IsError=%b : %s" argument (resultat.IsError = Nullable true) texte

appeler (box 4512)
appeler (box 9999)
appeler (box "abc")

client.DisposeAsync().AsTask().GetAwaiter().GetResult()
