# Local model project — beginner walkthrough

`AgentOrchestration.LocalModel` is a self-contained C# project that teaches
you the minimum you need to know to run a local AI model and connect it to the
main orchestration system.

## What you will learn

- How to check whether a local model server is running.
- How to list the models that are available on a machine.
- How to send a prompt and read a response over HTTP.
- How to implement `IAgentTaskRunner` so any local model can be dispatched by
  `AgentDispatchService`.

## Prerequisites

1. **Install .NET 10+** — the same requirement as the rest of this repository.
2. **Install Ollama** from <https://ollama.com>.
   - Windows: run the installer; Ollama starts automatically.
   - Linux: follow the shell installer instructions from the Ollama site.
3. **Pull a starter model** — open a terminal and run:

   ```bash
   ollama pull llama3.1:8b
   ```

   This downloads the model once (about 4 GB).  Subsequent runs use the cached
   copy.

4. Confirm Ollama is running:

   ```bash
   curl http://localhost:11434
   # Expected output: Ollama is running
   ```

   If Ollama stopped, restart it with `ollama serve`.

## Run the project

```bash
dotnet run --project src/AgentOrchestration.LocalModel
```

The program walks through four numbered steps printed to the console:

| Step | What it does |
| --- | --- |
| 1 | Calls `GET /` to confirm Ollama is reachable. |
| 2 | Calls `GET /api/tags` to list pulled models and picks the first one. |
| 3 | Calls `POST /api/generate` with a sample prompt and prints the response. |
| 4 | Wraps the whole thing in `AgentDispatchService` using `OllamaAgentRunner`. |

## Project structure

```
src/AgentOrchestration.LocalModel/
├── AgentOrchestration.LocalModel.csproj   project file
├── OllamaClient.cs                        typed HTTP wrapper for the Ollama API
├── OllamaAgentRunner.cs                   IAgentTaskRunner implementation
└── Program.cs                             step-by-step educational demo
```

### `OllamaClient`

Wraps three Ollama REST endpoints:

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `IsRunningAsync` | `GET /` | Health check |
| `ListModelsAsync` | `GET /api/tags` | List pulled models |
| `GenerateAsync` | `POST /api/generate` | Send a prompt, get a response |

No third-party NuGet packages are used — only `System.Net.Http.Json` from the
.NET standard library.

### `OllamaAgentRunner`

Implements `IAgentTaskRunner` from `AgentOrchestration.Core`.  Builds an
`OllamaClient` pointed at the node's endpoint, checks the health, and then
calls `GenerateAsync` with the task command as the prompt.

This is the adapter you swap into `AgentOrchestration.App` to replace
`DemoAgentTaskRunner` with real local inference:

```csharp
var runner = new OllamaAgentRunner("llama3.1:8b");
var result = await dispatcher.DispatchAsync(nodes, request, runner);
```

## Try different models

Pull any model from the Ollama library and update `selectedModel` in
`Program.cs`:

```bash
ollama pull qwen2.5:7b
ollama pull mistral:7b
ollama pull phi3:mini          # good for older GPUs
```

## Connect to a remote node

The `OllamaAgentRunner` reads `node.Endpoint` to build the base URL.  If you
run `ollama serve` on a second machine (say `192.168.1.44`) and add that
machine to the node list in `AgentOrchestration.App`, the dispatcher can
route work there automatically:

```csharp
new AgentNode(
    Name: "worker-1080ti",
    OperatingSystem: "Windows 11",
    ExecutionMode: "local",
    IsAvailable: true,
    HasGpu: true,
    Priority: 2,
    Endpoint: "192.168.1.44",      // OllamaAgentRunner uses this
    Tags: ["windows", "gpu", "ollama", "worker"])
```

## Next steps after this project

1. Replace the hardcoded prompt with user input from `Console.ReadLine()`.
2. Add a loop so you can have a multi-turn conversation.
3. Try streaming responses (`stream: true`) for a live typing effect.
4. Point `OllamaAgentRunner` at a second machine and observe failover in the
   main `AgentOrchestration.App` project.
5. Read `docs/local-model-setup.md` for hardware-specific model recommendations.
