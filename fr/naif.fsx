// naif.fsx — module 06 : ce qui se passe quand on désérialise directement les arguments du modèle.
open System.Text.Json

type ArgumentsCommande = { orderId: int }

let essayer (argsJson: string) =
    try
        printfn "%-20s -> %A" argsJson (JsonSerializer.Deserialize<ArgumentsCommande> argsJson)
    with ex ->
        printfn "%-20s -> exception %s" argsJson (ex.GetType().Name)

essayer """{"orderId":4512}"""
essayer """{}"""
essayer """{"orderId":"ABC-12"}"""
essayer """{"orderId":45"""
