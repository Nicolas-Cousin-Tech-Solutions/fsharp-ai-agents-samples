// statuts.fsx — module 03 : afficher la description de chaque commande d'exemple.
#load "commandes.fsx"

open Commandes

for commande in Map.values commandes do
    printfn "%s" (decrire commande)
