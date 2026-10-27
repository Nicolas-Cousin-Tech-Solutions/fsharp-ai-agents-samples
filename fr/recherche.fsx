// recherche.fsx — module 04 : chercher des commandes qui existent… ou pas.
#load "commandes.fsx"

open Commandes

printfn "%A" (trouverCommande 7781)
printfn "%A" (trouverCommande 9999)

for numero in [ 4512; 7781; 9999 ] do
    printfn "%s" (getOrder numero)
