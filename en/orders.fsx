// orders.fsx — the assistant's domain: pure F#, no package, no model.
// This file only declares types and functions: it prints nothing,
// because it is loaded by other scripts (including the MCP server in module 09).

open System

// Module 03 — a discriminated union: a status has one shape out of four,
// and each shape carries exactly the data that belongs to it.
type Status =
    | Processing
    | Shipped of tracking: string
    | Delivered of date: DateOnly
    | Cancelled of reason: string

type Order = { Id: int; Status: Status }

let orders =
    Map [
        (4512, { Id = 4512; Status = Shipped "TRK-88" })
        (7781, { Id = 7781; Status = Processing })
        (3020, { Id = 3020; Status = Delivered(DateOnly(2026, 10, 20)) })
        (6034, { Id = 6034; Status = Cancelled "out of stock" })
    ]

let describeStatus (status: Status) : string =
    match status with
    | Processing -> "processing"
    | Shipped tracking -> $"shipped, tracking {tracking}"
    | Delivered date -> $"""delivered on {date.ToString("yyyy-MM-dd")}"""
    | Cancelled reason -> $"cancelled ({reason})"

let describe (order: Order) : string =
    $"Order {order.Id}: {describeStatus order.Status}"

// Module 04 — Option: a lookup that may find nothing.
let findOrder (id: int) : Order option =
    Map.tryFind id orders

// The function the model will be able to ask us to call (modules 05 to 09).
let getOrder (orderId: int) : string =
    match findOrder orderId with
    | Some order -> describe order
    | None -> $"Order {orderId} not found"
