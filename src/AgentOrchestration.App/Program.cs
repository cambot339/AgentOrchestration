using AgentOrchestration.Core;

var nodes = new[]
{
    new AgentNode(
        Name: "main-4070",
        OperatingSystem: "Windows 11",
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: true,
        Priority: 1,
        Endpoint: "localhost",
        Tags: ["windows", "gpu", "ollama", "main"]),
    new AgentNode(
        Name: "worker-1080ti",
        OperatingSystem: "Windows 11",
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: true,
        Priority: 2,
        Endpoint: "192.168.1.44",
        Tags: ["windows", "gpu", "llama.cpp", "worker"]),
    new AgentNode(
        Name: "worker-1070",
        OperatingSystem: "Ubuntu",
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: true,
        Priority: 3,
        Endpoint: "192.168.1.52",
        Tags: ["linux", "gpu", "worker"]),
    new AgentNode(
        Name: "cloud-fallback",
        OperatingSystem: "Linux",
        ExecutionMode: "cloud",
        IsAvailable: true,
        HasGpu: false,
        Priority: 99,
        Endpoint: "https://cloud-agent.example.invalid",
        Tags: ["cloud", "fallback", "cpu"])
};

var request = new AgentTaskRequest(
    Name: "Local model smoke test",
    Command: "ollama run llama3.1:8b",
    PreferredTags: ["gpu", "ollama", "windows"]);

var dispatcher = new AgentDispatchService();
var runner = new DemoAgentTaskRunner();
var result = await dispatcher.DispatchAsync(nodes, request, runner);

Console.WriteLine("AgentOrchestration bootstrap");
Console.WriteLine("===========================");
Console.WriteLine("Purpose: start with a small, learnable runtime that can fan out local or cloud work and fail over when a node is unavailable.");
Console.WriteLine();
Console.WriteLine($"Task: {request.Name}");
Console.WriteLine($"Command: {request.Command}");
Console.WriteLine($"Status: {(result.Success ? "Succeeded" : "Failed")}");
Console.WriteLine($"Selected node: {result.NodeName ?? "none"}");
Console.WriteLine();
Console.WriteLine("Dispatch attempts:");
foreach (var attempt in result.Attempts)
{
    Console.WriteLine($"- {attempt.NodeName}: {(attempt.Success ? "ok" : "failed")} - {attempt.Message}");
}
Console.WriteLine();
Console.WriteLine("Next steps:");
Console.WriteLine("- Run the separate networking runtime from src/AgentOrchestration.Networking on each machine.");
Console.WriteLine("- Use the docs/local-model-setup.md guide to pick a local model runtime for each GPU.");
Console.WriteLine("- Replace the demo runner with real Copilot, local model, or custom agent adapters as you learn.");

sealed class DemoAgentTaskRunner : IAgentTaskRunner
{
    public Task RunAsync(AgentNode node, AgentTaskRequest request, CancellationToken cancellationToken)
    {
        if (node.Name == "main-4070")
        {
            throw new InvalidOperationException("Primary workstation is busy; trying the next available agent.");
        }

        return Task.CompletedTask;
    }
}
