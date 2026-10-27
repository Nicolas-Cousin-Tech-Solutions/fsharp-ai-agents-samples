// demander.fsx — module 07 : poser de vraies questions à l'agent (modèle local).
#load "agent.fsx"

open Agent

let questions =
    [ "Où en est ma commande 4512 ?"
      "Où en est ma commande 9999 ?" ]

for question in questions do
    printfn "=== %s" question

    match executerAgent Llm.client question with
    | Ok texte -> printfn "Réponse : %s" texte
    | Error erreur -> printfn "Arrêt : %s" (decrireErreur erreur)
