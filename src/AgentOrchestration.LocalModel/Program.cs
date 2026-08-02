// AgentOrchestration.LocalModel — beginner project
// ===================================================
// This project teaches you how to run a local AI model using Ollama and how
// to connect it to the main AgentOrchestration dispatcher.
//
// BEFORE RUNNING THIS PROJECT:
//   1. Install Ollama from https://ollama.com (Windows, macOS, or Linux).
//   2. Open a terminal and run:  ollama pull llama3.1:8b
//      (This downloads the model once — about 4 GB.)
//   3. Ollama starts automatically after installation.  If it is not running,
//      start it with:  ollama serve
//   4. Run this project with:
//      dotnet run --project src/AgentOrchestration.LocalModel

using AgentOrchestration.Core;
using AgentOrchestration.LocalModel;

Console.WriteLine("AgentOrchestration.LocalModel — local model basics");
Console.WriteLine("====================================================");
Console.WriteLine();

var ollamaUrl = "http://localhost:11434";
var client = new OllamaClient(ollamaUrl);

// ---------------------------------------------------------------------------
// STEP 1: Check if Ollama is running
// ---------------------------------------------------------------------------
// The very first thing to do is confirm the local model server is up.
// If this fails, you need to start Ollama before continuing.
Console.WriteLine("Step 1: checking Ollama health...");
var isRunning = await client.IsRunningAsync();

if (!isRunning)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Ollama is not running at {ollamaUrl}.");
    Console.WriteLine("  Start it with 'ollama serve' and then re-run this project.");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("  Ollama is running.");
Console.ResetColor();
Console.WriteLine();

// ---------------------------------------------------------------------------
// STEP 2: List available models
// ---------------------------------------------------------------------------
// Ollama keeps a local library of models you have pulled.  You must pull a
// model with 'ollama pull <name>' before you can generate text with it.
Console.WriteLine("Step 2: listing available models...");
var models = await client.ListModelsAsync();

if (models.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  No models found.  Pull one with:  ollama pull llama3.1:8b");
    Console.ResetColor();
    Console.WriteLine();
}
else
{
    foreach (var model in models)
    {
        var sizeGb = model.Size / 1_073_741_824.0;
        Console.WriteLine($"  {model.Name}  ({sizeGb:F1} GB)");
    }

    Console.WriteLine();
}

// Choose the first available model or fall back to the recommended default.
var selectedModel = models.FirstOrDefault()?.Name ?? "llama3.1:8b";
Console.WriteLine($"Using model: {selectedModel}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// STEP 3: Send a prompt and read the response
// ---------------------------------------------------------------------------
// GenerateAsync posts a prompt to Ollama and waits for the complete response.
// Streaming is disabled so the entire reply arrives as a single string.
// This is the simplest way to start — add streaming later when you are ready.
var prompt = "In one sentence, what is an AI agent?";

Console.WriteLine($"Step 3: sending a prompt to {selectedModel}...");
Console.WriteLine($"  Prompt: {prompt}");
Console.WriteLine();

string reply;
try
{
    reply = await client.GenerateAsync(selectedModel, prompt);
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Request failed: {ex.Message}");
    Console.WriteLine($"  Make sure '{selectedModel}' has been pulled with:  ollama pull {selectedModel}");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"  Response: {reply.Trim()}");
Console.ResetColor();
Console.WriteLine();

// ---------------------------------------------------------------------------
// STEP 4: Use OllamaAgentRunner with the main dispatcher
// ---------------------------------------------------------------------------
// OllamaAgentRunner implements IAgentTaskRunner, which is the interface that
// AgentDispatchService from AgentOrchestration.Core expects.
// This shows how the local model project connects to the orchestration layer.
Console.WriteLine("Step 4: dispatching through AgentDispatchService with OllamaAgentRunner...");
Console.WriteLine();

var nodes = new[]
{
    new AgentNode(
        Name: "local-ollama",
        OperatingSystem: System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: true,
        Priority: 1,
        Endpoint: "localhost",
        Tags: ["local", "ollama", "gpu"])
};

var request = new AgentTaskRequest(
    Name: "AI agent definition",
    Command: prompt,
    PreferredTags: ["local", "ollama"]);

var runner = new OllamaAgentRunner(selectedModel);
var dispatcher = new AgentDispatchService();
var result = await dispatcher.DispatchAsync(nodes, request, runner);

Console.WriteLine();
Console.WriteLine($"Dispatch result: {(result.Success ? "succeeded" : "failed")}");
Console.WriteLine($"Node used: {result.NodeName ?? "none"}");
Console.WriteLine();
Console.WriteLine("Next steps:");
Console.WriteLine("- Try different models:  ollama pull qwen2.5:7b  then change selectedModel.");
Console.WriteLine("- Change the prompt to anything you want the model to answer.");
Console.WriteLine("- Add more nodes to the array to test failover.");
Console.WriteLine("- Swap DemoAgentTaskRunner in AgentOrchestration.App with OllamaAgentRunner");
Console.WriteLine("  to connect real local inference to the full multi-machine dispatcher.");
