// outil.fsx — module 05 : un aller-retour d'outil, fait à la main.
#load "llm.fsx" "commandes.fsx"

open System
open System.Text.Json
open Microsoft.Extensions.AI
open Llm
open Commandes

verifierOllama ()

// 1. Décrire l'outil au modèle : nom, description, schéma (déduit de getOrder).
let outilCommande =
    AIFunctionFactory.Create(
        Func<int, string>(getOrder),
        "getOrder",
        "Retourne le statut d'une commande à partir de son numéro."
    )

printfn "[schéma envoyé au modèle] %s" (outilCommande.JsonSchema.GetRawText())

let options = creerOptions ()
options.Tools <- ResizeArray<AITool> [ outilCommande :> AITool ]

let instructions =
    "Tu es l'assistant support d'une boutique en ligne. "
    + "Utilise les outils pour répondre. Réponds en français, brièvement."

let historique =
    ResizeArray<ChatMessage>
        [ ChatMessage(ChatRole.System, instructions)
          ChatMessage(ChatRole.User, "Où en est ma commande 4512 ?") ]

// 2. Premier tour : le modèle répond… par une demande d'outil, ou par du texte.
let reponse1 = client.GetResponseAsync(historique, options).GetAwaiter().GetResult()

let demandes =
    reponse1.Messages
    |> Seq.collect (fun message -> message.Contents)
    |> Seq.choose (fun contenu ->
        match contenu with
        | :? FunctionCallContent as demande -> Some demande
        | _ -> None)
    |> List.ofSeq

printfn "[tour 1] demandes d'outil : %d" demandes.Length

if demandes.IsEmpty then
    printfn "[tour 1] texte du modèle : %s" reponse1.Text
else
    // 3. Votre code exécute l'outil, puis ajoute le résultat à l'historique.
    historique.AddRange(reponse1.Messages)

    for demande in demandes do
        let argsJson = JsonSerializer.Serialize demande.Arguments
        printfn "[le modèle demande] %s %s" demande.Name argsJson
        // Lecture naïve : le module 06 remplacera ce cast par une vraie frontière.
        let numero = (demande.Arguments["orderId"] :?> JsonElement).GetInt32()
        let resultat = getOrder numero
        printfn "[votre code exécute] getOrder %d -> %s" numero resultat
        let contenu = FunctionResultContent(demande.CallId, resultat) :> AIContent
        historique.Add(ChatMessage(ChatRole.Tool, ResizeArray<AIContent> [ contenu ]))

    // 4. Second tour : le modèle lit le résultat et formule la réponse finale.
    let reponse2 = client.GetResponseAsync(historique, options).GetAwaiter().GetResult()
    printfn "[tour 2] réponse finale : %s" reponse2.Text
