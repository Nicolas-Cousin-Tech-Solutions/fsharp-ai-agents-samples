// llm.fsx — le SEUL fichier qui sait quel fournisseur et quel modèle vous utilisez.
// Testé le 2026-10-27 avec .NET SDK 10.0.401, Ollama 0.34.0, qwen3.5:4b.
#r "nuget: Microsoft.Extensions.AI, 10.11.0"
#r "nuget: OllamaSharp, 5.5.0"

open System
open System.Net.Http
open Microsoft.Extensions.AI
open OllamaSharp

// Adresse du serveur Ollama : localhost par défaut, ou la variable d'environnement OLLAMA_URL.
let adresse =
    match Environment.GetEnvironmentVariable "OLLAMA_URL" with
    | null
    | "" -> "http://localhost:11434"
    | url -> url

let modele = "qwen3.5:4b"

// Construit un IChatClient. Le délai est explicite : sur CPU, une réponse peut prendre des dizaines de secondes.
let creerClient (adresseServeur: string) (delai: TimeSpan) : IChatClient =
    let http = new HttpClient(BaseAddress = Uri adresseServeur, Timeout = delai)
    new OllamaApiClient(http, modele)

// Le client utilisé par tous les scripts du cours.
let client: IChatClient = creerClient adresse (TimeSpan.FromMinutes 3.0)

// Options de base : pas de « réflexion » (thinking), température 0.
let creerOptions () : ChatOptions =
    ChatOptions(
        Reasoning = ReasoningOptions(Effort = ReasoningEffort.None),
        Temperature = Nullable 0.0f
    )
