// llm.fsx — the ONLY file that knows which provider and which model you use.
// Tested on 2026-10-27 with .NET SDK 10.0.401, Ollama 0.34.0, qwen3.5:4b.
#r "nuget: Microsoft.Extensions.AI, 10.11.0"
#r "nuget: OllamaSharp, 5.5.0"

open System
open System.Net.Http
open Microsoft.Extensions.AI
open OllamaSharp

// Ollama address: localhost by default, or the OLLAMA_URL environment variable.
let address =
    match Environment.GetEnvironmentVariable "OLLAMA_URL" with
    | null
    | "" -> "http://localhost:11434"
    | url -> url

let model = "qwen3.5:4b"

// Builds an IChatClient. The timeout is explicit: on CPU, a response
// can take tens of seconds.
let createClient (serverAddress: string) (timeout: TimeSpan) : IChatClient =
    let http = new HttpClient(BaseAddress = Uri serverAddress, Timeout = timeout)
    new OllamaApiClient(http, model)

// The client used by every script in the course.
let client: IChatClient = createClient address (TimeSpan.FromMinutes 3.0)

// Base options: no "thinking", temperature 0.
let createOptions () : ChatOptions =
    ChatOptions(
        Reasoning = ReasoningOptions(Effort = ReasoningEffort.None),
        Temperature = Nullable 0.0f
    )

// Checks that Ollama answers and that the model is downloaded. Otherwise, says
// what to do and stops the script, instead of printing a stack trace.
let checkOllama () =
    let problem =
        try
            let http = new HttpClient(BaseAddress = Uri address, Timeout = TimeSpan.FromSeconds 10.0)
            use ollama = new OllamaApiClient(http)
            let models = ollama.ListLocalModelsAsync().GetAwaiter().GetResult()

            if models |> Seq.exists (fun m -> m.Name = model) then
                None
            else
                Some $"the model {model} is not downloaded. Run: ollama pull {model}"
        with
        | :? HttpRequestException
        | :? Threading.Tasks.TaskCanceledException ->
            Some $"Ollama does not answer on {address}. Start it (Ollama app or \"ollama serve\")."

    match problem with
    | None -> ()
    | Some message ->
        eprintfn "Cannot continue: %s" message
        exit 1
