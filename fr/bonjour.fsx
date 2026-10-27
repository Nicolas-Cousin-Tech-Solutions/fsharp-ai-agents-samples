// bonjour.fsx — module 02 : une première question au modèle local.
#load "llm.fsx"

open System.Diagnostics
open Microsoft.Extensions.AI
open Llm

verifierOllama ()

let question = "En une phrase, qu'est-ce qu'un agent IA ?"

let chrono = Stopwatch.StartNew()
let reponse = client.GetResponseAsync(question, creerOptions ()).GetAwaiter().GetResult()

printfn "Modèle  : %s" modele
printfn "Réponse : %s" reponse.Text
printfn "Durée   : %.1f s" chrono.Elapsed.TotalSeconds
