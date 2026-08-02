using AgentOrchestration.Core;
using AgentOrchestration.LocalModels;

var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.1:8b";
var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var prompt = args.Length > 0
    ? string.Join(' ', args)
    : "Explain what an AI agent is in three beginner-friendly sentences.";

var node = new AgentNode(
    Name: "local-ollama",
    OperatingSystem: Environment.OSVersion.Platform.ToString(),
    ExecutionMode: model,
    IsAvailable: true,
    HasGpu: false,
    Priority: 1,
    Endpoint: endpoint,
    Tags: ["local", "ollama"]);

var request = new AgentTaskRequest(
    Name: "Beginner local model prompt",
    Command: prompt,
    PreferredTags: ["local", "ollama"]);

Console.WriteLine($"Model: {model}");
Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Prompt: {prompt}");
Console.WriteLine();

try
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    var runner = new OllamaTaskRunner(httpClient);
    await runner.RunAsync(node, request, CancellationToken.None);
}
catch (HttpRequestException)
{
    Console.Error.WriteLine(
        "Could not reach Ollama. Start it first, then confirm OLLAMA_ENDPOINT is correct.");
    Environment.ExitCode = 1;
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}
