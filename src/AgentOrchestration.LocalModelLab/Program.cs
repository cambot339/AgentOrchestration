using System.Net.Http.Json;
using System.Text.Json;
using AgentOrchestration.Core;

var settings = LocalModelSettings.Parse(args);

var nodes = new[]
{
    new AgentNode(
        Name: "local-ollama",
        OperatingSystem: Environment.OSVersion.VersionString,
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: true,
        Priority: 1,
        Endpoint: settings.BaseUrl,
        Tags: ["ollama", "local", "model-lab"]),
    new AgentNode(
        Name: "cloud-fallback",
        OperatingSystem: "Linux",
        ExecutionMode: "cloud",
        IsAvailable: true,
        HasGpu: false,
        Priority: 99,
        Endpoint: "https://cloud-agent.example.invalid",
        Tags: ["cloud", "fallback"])
};

var request = new AgentTaskRequest(
    Name: "Local model lab request",
    Command: settings.Prompt,
    PreferredTags: ["ollama", "local", "model-lab"]);

var dispatcher = new AgentDispatchService();
var runner = new OllamaAgentTaskRunner(settings.Model);
var result = await dispatcher.DispatchAsync(nodes, request, runner);

Console.WriteLine("AgentOrchestration.LocalModelLab");
Console.WriteLine("================================");
Console.WriteLine("Use this project to learn the basics of calling a local model through the same dispatch concepts used by the main project.");
Console.WriteLine();
Console.WriteLine($"Model: {settings.Model}");
Console.WriteLine($"Base URL: {settings.BaseUrl}");
Console.WriteLine($"Selected node: {result.NodeName ?? "none"}");
Console.WriteLine($"Status: {(result.Success ? "Succeeded" : "Failed")}");
Console.WriteLine();
Console.WriteLine("Dispatch attempts:");
foreach (var attempt in result.Attempts)
{
    Console.WriteLine($"- {attempt.NodeName}: {(attempt.Success ? "ok" : "failed")} - {attempt.Message}");
}
Console.WriteLine();
Console.WriteLine("Next steps:");
Console.WriteLine("- Start Ollama before running this sample.");
Console.WriteLine("- Try a different --model or --prompt value.");
Console.WriteLine("- When you are ready, move this runner into a shared adapter for the main app.");

sealed class OllamaAgentTaskRunner(string model) : IAgentTaskRunner
{
    public async Task RunAsync(AgentNode node, AgentTaskRequest request, CancellationToken cancellationToken)
    {
        if (!node.Tags.Contains("ollama", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Node '{node.Name}' is not configured for Ollama.");
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(AppendTrailingSlash(node.Endpoint))
        };

        var response = await httpClient.PostAsJsonAsync(
            "api/generate",
            new OllamaGenerateRequest(model, request.Command, false),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Ollama returned {(int)response.StatusCode}: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Response))
        {
            throw new InvalidOperationException("Ollama returned an empty response.");
        }

        Console.WriteLine("Local model response");
        Console.WriteLine("--------------------");
        Console.WriteLine(payload.Response.Trim());
        Console.WriteLine();
    }

    private static string AppendTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
}

sealed record OllamaGenerateRequest(
    string Model,
    string Prompt,
    bool Stream);

sealed record OllamaGenerateResponse(
    string Response,
    [property: JsonPropertyName("done")] bool Done);

sealed record LocalModelSettings(string Model, string Prompt, string BaseUrl)
{
    public static LocalModelSettings Parse(string[] args)
    {
        const string defaultPrompt = "Explain agent orchestration in three short bullet points for a beginner.";
        var model = "llama3.1:8b";
        var prompt = defaultPrompt;
        var baseUrl = "http://localhost:11434";

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--model" when index + 1 < args.Length:
                    model = args[++index];
                    break;
                case "--prompt" when index + 1 < args.Length:
                    prompt = args[++index];
                    break;
                case "--base-url" when index + 1 < args.Length:
                    baseUrl = args[++index];
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return new LocalModelSettings(model, prompt, baseUrl);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project /home/runner/work/AgentOrchestration/AgentOrchestration/src/AgentOrchestration.LocalModelLab -- [--model llama3.1:8b] [--base-url http://localhost:11434] [--prompt \"Explain agent orchestration\"]");
    }
}
