// echantillons.fsx — module 06 : demandes d'outil brutes, comme en enverrait un modèle.
#load "decodage.fsx"

open Decodage

let echantillons =
    [
        ("getOrder", """{"orderId":4512}""")
        ("cancelOrder", """{"orderId":7781}""")
        ("getOrder", """{}""")
        ("getOrder", """{"orderId":"ABC-12"}""")
        ("getOrder", """{"orderId":45""")
    ]

for (nom, argsJson) in echantillons do
    printfn "%-11s %-20s -> %A" nom argsJson (decoder nom argsJson)
