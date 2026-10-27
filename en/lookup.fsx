// lookup.fsx — module 04: look up orders that exist… or not.
#load "orders.fsx"

open Orders

printfn "%A" (findOrder 7781)
printfn "%A" (findOrder 9999)

for number in [ 4512; 7781; 9999 ] do
    printfn "%s" (getOrder number)
