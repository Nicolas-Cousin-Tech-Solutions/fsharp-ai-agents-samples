// agent.fsx — modules 07 et 08 : boucle d'agent bornée ; chaque tour est une union discriminée.
// Ce fichier définit l'agent sans le lancer : demander.fsx et scenarios.fsx l'utilisent.
#load "llm.fsx" "commandes.fsx" "decodage.fsx"

open System
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.AI
open Llm
open Commandes
open Decodage

// Ce que le modèle a produit à ce tour : une forme parmi trois.
type AgentStep =
    | FinalAnswer of text: string
    | CallTools of calls: (string * Result<ToolCall, ToolError>) list
    | InvalidResponse of reason: string

// Pourquoi l'agent s'est arrêté sans réponse finale.
type AgentError =
    | TurnLimitReached of maxTurns: int
    | InvalidModelResponse of reason: string
    | ModelUnreachable of detail: string
    | ModelTimeout

let decrireErreur (erreur: AgentError) : string =
    match erreur with
    | TurnLimitReached limite -> $"limite de {limite} tours atteinte"
    | InvalidModelResponse raison -> $"réponse du modèle inutilisable ({raison})"
    | ModelUnreachable _ -> "serveur du modèle injoignable"
    | ModelTimeout -> "délai d'attente du modèle dépassé"

let maxTours = 4

let instructions =
    "Tu es l'assistant support d'une boutique en ligne. "
    + "Utilise les outils pour répondre. Réponds en français, brièvement."

let outilCommande =
    AIFunctionFactory.Create(
        Func<int, string>(getOrder),
        "getOrder",
        "Retourne le statut d'une commande à partir de son numéro."
    )

// Adaptateur : une demande d'outil .NET -> la frontière du module 06.
let decoderDemande (demande: FunctionCallContent) : Result<ToolCall, ToolError> =
    decoder demande.Name (JsonSerializer.Serialize demande.Arguments)

let classer (reponse: ChatResponse) : AgentStep =
    let demandes =
        reponse.Messages
        |> Seq.collect (fun message -> message.Contents)
        |> Seq.choose (fun contenu ->
            match contenu with
            | :? FunctionCallContent as demande -> Some demande
            | _ -> None)
        |> List.ofSeq

    if not demandes.IsEmpty then
        CallTools [ for demande in demandes -> (demande.CallId, decoderDemande demande) ]
    elif String.IsNullOrWhiteSpace reponse.Text then
        InvalidResponse "réponse vide"
    else
        FinalAnswer(reponse.Text.Trim())

// Seuls les cas de ToolCall peuvent être exécutés : c'est la liste autorisée.
let executer (demande: Result<ToolCall, ToolError>) : string =
    match demande with
    | Ok(GetOrder numero) -> getOrder numero
    | Error erreur -> sprintf "Erreur : %A" erreur

// Module 08 — les pannes de transport deviennent des cas nommés.
let appelerModele
    (client: IChatClient)
    (historique: ResizeArray<ChatMessage>)
    (options: ChatOptions)
    =
    try
        Ok(client.GetResponseAsync(historique, options).GetAwaiter().GetResult())
    with
    | :? HttpRequestException as ex -> Error(ModelUnreachable ex.Message)
    | :? TaskCanceledException -> Error ModelTimeout

let executerAgent (client: IChatClient) (question: string) : Result<string, AgentError> =
    let options = creerOptions ()
    options.Tools <- ResizeArray<AITool> [ outilCommande :> AITool ]

    let historique =
        ResizeArray<ChatMessage>
            [ ChatMessage(ChatRole.System, instructions)
              ChatMessage(ChatRole.User, question) ]

    let rec boucle tour =
        if tour > maxTours then
            Error(TurnLimitReached maxTours)
        else
            match appelerModele client historique options with
            | Error erreur -> Error erreur
            | Ok reponse ->
                match classer reponse with
                | FinalAnswer texte ->
                    printfn "[tour %d] FinalAnswer" tour
                    Ok texte
                | InvalidResponse raison ->
                    printfn "[tour %d] InvalidResponse %s" tour raison
                    Error(InvalidModelResponse raison)
                | CallTools demandes ->
                    printfn "[tour %d] CallTools %A" tour (List.map snd demandes)
                    historique.AddRange(reponse.Messages)

                    for (idAppel, demande) in demandes do
                        let resultat = executer demande
                        printfn "         -> %s" resultat
                        let contenu = FunctionResultContent(idAppel, resultat) :> AIContent
                        historique.Add(ChatMessage(ChatRole.Tool, ResizeArray<AIContent> [ contenu ]))

                    boucle (tour + 1)

    boucle 1
