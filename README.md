# F# AI agent samples

Build an AI agent in F# — no Python, no API key, no project file.

Every sample is a plain F# script run with `dotnet fsi`, against a **local Ollama model that runs on CPU**. Together they build a small e-commerce support agent that answers "where is my order?" by calling a .NET tool, then expose that tool through an **MCP server written in a single `.fsx` file**.

These scripts are the companion code of the French course *C# vers F# : construire un agent IA sans passer par Python* (Académie NCTS, publishing soon). Identifiers and comments are in French, but the code reads on its own.

## Why F# for agents

Building an agent is not training a model. It is calling an API, parsing JSON, running tools and handling failures — everyday .NET work. F# adds three things that fit this job well:

- **Scripts as light as Python**: `#r "nuget: ..."` in an `.fsx`, no project, no `Main`.
- **Discriminated unions** for each agent step (final answer / tool calls / invalid response): the compiler flags the case you forgot.
- **`Result` at the model boundary**: a model invents tools, drops arguments or sends malformed JSON. Decoding into `Result` turns that into values the code must handle.

A gotcha worth knowing: on .NET 10, `System.Text.Json` silently turns a missing required field into `0` when deserializing an F# record, and does not support discriminated unions. `naif.fsx` shows the problem, `decodage.fsx` the hand-written decoder that replaces it.

## Prerequisites

- .NET SDK 10
- [Ollama](https://ollama.com), then `ollama pull qwen3.5:4b` (about 3.4 GB)
- No GPU needed. On CPU, expect a few seconds to a few tens of seconds per model call, and a slower first call while the model loads.

The scripts target `http://localhost:11434`. To use another Ollama server, set `OLLAMA_URL`:

```bash
OLLAMA_URL=http://my-server:11434 dotnet fsi bonjour.fsx
```

## The scripts

Run them in order with `dotnet fsi <script>.fsx`.

| Script | What it shows | Needs the model |
|---|---|---|
| `bonjour.fsx` | First question to the local model through `Microsoft.Extensions.AI` | yes |
| `statuts.fsx` | Order status modelled as a discriminated union, exhaustive `match` | no |
| `recherche.fsx` | Looking up an order that may not exist: `Option` | no |
| `outil.fsx` | One tool round trip done by hand: the model asks, your code runs `getOrder` | yes |
| `naif.fsx` | What goes wrong when you deserialize model arguments directly | no |
| `echantillons.fsx` | Raw tool requests decoded into `Result`, including malformed ones | no |
| `demander.fsx` | The agent loop: bounded turns, each step a discriminated union | yes |
| `scenarios.fsx` | Replaying failures reproducibly (unknown tool, bad arguments, server down, timeout) with a scripted fake client | server only (scenario 5) |
| `serveur-mcp.fsx` | An MCP server exposing `getOrder`, in one script | — |
| `client-mcp.fsx` | Starts the server, lists its tools and calls `getOrder` | no |

"Server only" means Ollama must be running, but no answer from the model is used: the timeout scenario needs a reachable server to time out against. Without one, it reports "server unreachable" instead.

Shared files, loaded by the scripts above (they print nothing on their own):

- `llm.fsx` — the only file that knows the provider and the model. Thinking mode is turned off (`ReasoningEffort.None`), which is about 10× faster on CPU.
- `commandes.fsx` — the order domain, pure F#, no package.
- `decodage.fsx` — the boundary between model data and your code.
- `agent.fsx` — the agent loop.

## Switching model or provider

Only `llm.fsx` changes. Swapping the `modele` value for another Ollama model with tool support is enough; the rest of the code stays the same.

## Tested versions

Tested on 2026-10-27 with .NET SDK 10.0.401, Ollama 0.34.0 and `qwen3.5:4b` on CPU:

- `Microsoft.Extensions.AI` 10.11.0
- `OllamaSharp` 5.5.0 (`Microsoft.Extensions.AI.Ollama` is deprecated — avoid older tutorials that use it)
- `ModelContextProtocol` 2.3.0
- `Microsoft.Extensions.Hosting` 10.0.14

Package versions are pinned in the `#r` lines. The .NET LLM ecosystem moves fast: if something breaks, check these versions first.
