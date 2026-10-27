// commandes.fsx — le domaine de l'assistant : du F# pur, sans package, sans modèle.
// Ce fichier ne fait que déclarer des types et des fonctions : il n'affiche rien,
// car il est chargé par d'autres scripts (dont le serveur MCP du module 09).

open System

// Module 03 — une union discriminée : un statut a une forme parmi quatre,
// et chaque forme porte exactement les données qui la concernent.
type Statut =
    | EnPreparation
    | Expediee of suivi: string
    | Livree of date: DateOnly
    | Annulee of motif: string

type Commande = { Id: int; Statut: Statut }

let commandes =
    Map [
        (4512, { Id = 4512; Statut = Expediee "TRK-88" })
        (7781, { Id = 7781; Statut = EnPreparation })
        (3020, { Id = 3020; Statut = Livree(DateOnly(2026, 10, 20)) })
        (6034, { Id = 6034; Statut = Annulee "rupture de stock" })
    ]

let decrireStatut (statut: Statut) : string =
    match statut with
    | EnPreparation -> "en préparation"
    | Expediee suivi -> $"expédiée, suivi {suivi}"
    | Livree date -> $"""livrée le {date.ToString("yyyy-MM-dd")}"""
    | Annulee motif -> $"annulée ({motif})"

let decrire (commande: Commande) : string =
    $"Commande {commande.Id} : {decrireStatut commande.Statut}"

// Module 04 — Option : une recherche qui peut ne rien trouver.
let trouverCommande (id: int) : Commande option =
    Map.tryFind id commandes

// La fonction que le modèle pourra demander d'appeler (modules 05 à 09).
let getOrder (orderId: int) : string =
    match trouverCommande orderId with
    | Some commande -> decrire commande
    | None -> $"Commande {orderId} introuvable"
