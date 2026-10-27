// decodage.fsx — module 06 : la frontière entre les données du modèle et votre code.
// Aucun package : System.Text.Json fait partie de .NET.
// Ce fichier n'affiche rien : il est chargé par echantillons.fsx et agent.fsx.

open System.Text.Json

// Ce que votre code accepte d'exécuter : la liste fermée des outils autorisés.
type ToolCall =
    | GetOrder of orderId: int

// Tout ce qui peut être faux dans une demande d'outil, avec un nom.
type ToolError =
    | UnknownTool of name: string
    | InvalidJson of raw: string
    | MissingArgument of name: string
    | BadArgumentType of name: string * received: string

// Étape 1 — le nom d'outil fait-il partie de la liste autorisée ?
let verifierNomOutil (nom: string) : Result<string, ToolError> =
    if nom = "getOrder" then Ok nom else Error(UnknownTool nom)

// Étape 2 — le texte des arguments est-il un objet JSON ?
let lireJson (argsJson: string) : Result<JsonElement, ToolError> =
    try
        use document = JsonDocument.Parse argsJson
        if document.RootElement.ValueKind = JsonValueKind.Object then
            Ok(document.RootElement.Clone())
        else
            Error(InvalidJson argsJson)
    with :? JsonException ->
        Error(InvalidJson argsJson)

// Étape 3 — l'argument orderId est-il présent, et est-ce un entier ?
let lireOrderId (racine: JsonElement) : Result<int, ToolError> =
    match racine.TryGetProperty "orderId" with
    | false, _ -> Error(MissingArgument "orderId")
    | true, valeur ->
        let estUnEntier =
            valeur.ValueKind = JsonValueKind.Number && fst (valeur.TryGetInt32())
        if estUnEntier then
            Ok(valeur.GetInt32())
        else
            Error(BadArgumentType("orderId", valeur.ToString()))

// Le pipeline : la première erreur court-circuite les étapes suivantes.
let decoder (nom: string) (argsJson: string) : Result<ToolCall, ToolError> =
    verifierNomOutil nom
    |> Result.bind (fun _ -> lireJson argsJson)
    |> Result.bind lireOrderId
    |> Result.map GetOrder
