// scenarios.fsx — module 08 : rejouer des pannes, de façon reproductible.
#load "agent.fsx"

open System
open System.Collections.Generic
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.AI
open Agent

// ---------------------------------------------------------------------------
// FOURNI — un faux IChatClient qui rejoue des réponses écrites à l'avance.
// Ce n'est pas une leçon de test : c'est un « lecteur de scénario » qui produit,
// à coup sûr, des réponses que le vrai modèle ne produit pas sur commande.
let fauxClient (reponses: ChatResponse list) : IChatClient =
    let file = Queue<ChatResponse>(reponses)

    { new IChatClient with
        member _.GetResponseAsync(_, _, _) = Task.FromResult(file.Dequeue())
        member _.GetStreamingResponseAsync(_, _, _) = raise (NotSupportedException())
        member _.GetService(_, _) = null
        member _.Dispose() = () }

// Réponse du « modèle » qui demande un outil (arguments en JsonElement, comme Ollama).
let demandeOutil (nom: string) (argsJson: string) : ChatResponse =
    let args = JsonSerializer.Deserialize<Dictionary<string, obj>>(argsJson)
    let demande = FunctionCallContent("appel-1", nom, args) :> AIContent
    ChatResponse(ChatMessage(ChatRole.Assistant, ResizeArray<AIContent> [ demande ]))

let texte (contenu: string) : ChatResponse =
    ChatResponse(ChatMessage(ChatRole.Assistant, contenu))
// ---------------------------------------------------------------------------

let afficherIssue (issue: Result<string, AgentError>) =
    match issue with
    | Ok reponse -> printfn "Issue : Ok \"%s\"" reponse
    | Error erreur -> printfn "Issue : Error — %s" (decrireErreur erreur)

let scenario (titre: string) (client: IChatClient) =
    printfn "=== %s" titre
    afficherIssue (executerAgent client "Où en est ma commande 7781 ?")

scenario
    "1. Outil hors liste autorisée"
    (fauxClient [ demandeOutil "cancelOrder" """{"orderId":7781}"""
                  texte "Je ne peux pas annuler de commande." ])

scenario
    "2. Argument mal typé"
    (fauxClient [ demandeOutil "getOrder" """{"orderId":"ABC-12"}"""
                  texte "Pouvez-vous me donner un numéro de commande ?" ])

scenario
    "3. Le modèle redemande l'outil sans fin"
    (fauxClient (List.replicate 10 (demandeOutil "getOrder" """{"orderId":7781}""")))

scenario
    "4. Serveur du modèle arrêté"
    (Llm.creerClient "http://localhost:1" (TimeSpan.FromMinutes 3.0))

scenario
    "5. Délai trop court"
    (Llm.creerClient Llm.adresse (TimeSpan.FromMilliseconds 1.0))
