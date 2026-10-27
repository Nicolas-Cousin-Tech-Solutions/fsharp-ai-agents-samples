// statuses.fsx — module 03: print the description of each sample order.
#load "orders.fsx"

open Orders

for order in Map.values orders do
    printfn "%s" (describe order)
