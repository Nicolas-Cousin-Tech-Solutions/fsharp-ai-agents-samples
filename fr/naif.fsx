// naif.fsx — module 06 : désérialiser directement les arguments du modèle.
open System.Text.Json

type ArgumentsCommande = { orderId: int }

let essayer (argsJson: string) =
    try
        let valeur = JsonSerializer.Deserialize<ArgumentsCommande> argsJson
        printfn "%-20s -> %A" argsJson valeur
    with ex ->
        printfn "%-20s -> exception %s" argsJson (ex.GetType().Name)

essayer """{"orderId":4512}"""
essayer """{}"""
essayer """{"orderId":"ABC-12"}"""
essayer """{"orderId":45"""
