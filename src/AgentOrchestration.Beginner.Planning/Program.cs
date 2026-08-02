using AgentOrchestration.Core;

var nodes = new[]
{
    new AgentNode(
        Name: "planner-laptop",
        OperatingSystem: "Windows 11",
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: false,
        Priority: 1,
        Endpoint: "prompt-workshop",
        Tags: ["planning", "local", "beginner"]),
    new AgentNode(
        Name: "cloud-fallback",
        OperatingSystem: "Linux",
        ExecutionMode: "cloud",
        IsAvailable: true,
        HasGpu: false,
        Priority: 99,
        Endpoint: "https://cloud-agent.example.invalid",
        Tags: ["planning", "cloud", "fallback"])
};

var request = new AgentTaskRequest(
    Name: "Beginner planning lesson",
    Command:
    """
    Create a beginner-friendly implementation plan for a console reading-list app.
    Return:
    1. the goal,
    2. small build steps,
    3. important edge cases,
    4. the first coding task to start with.
    """,
    PreferredTags: ["planning", "beginner", "local"]);

var dispatcher = new AgentDispatchService();
var runner = new PlanningLessonRunner();
var result = await dispatcher.DispatchAsync(nodes, request, runner);

Console.WriteLine("AgentOrchestration.Beginner.Planning");
Console.WriteLine("====================================");
Console.WriteLine("Goal: learn how to ask an AI agent for a clear plan before writing code.");
Console.WriteLine();
Console.WriteLine($"Task: {request.Name}");
Console.WriteLine($"Selected node: {result.NodeName ?? "none"}");
Console.WriteLine($"Status: {(result.Success ? "Ready" : "Blocked")}");
Console.WriteLine();
Console.WriteLine("Dispatch attempts:");
foreach (var attempt in result.Attempts)
{
    Console.WriteLine($"- {attempt.NodeName}: {(attempt.Success ? "ok" : "failed")} - {attempt.Message}");
}

sealed class PlanningLessonRunner : IAgentTaskRunner
{
    public Task RunAsync(AgentNode node, AgentTaskRequest request, CancellationToken cancellationToken)
    {
        if (!node.Tags.Contains("planning", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Node '{node.Name}' is not configured for planning lessons.");
        }

        Console.WriteLine("Suggested planning prompt");
        Console.WriteLine("-------------------------");
        Console.WriteLine(request.Command);
        Console.WriteLine();
        Console.WriteLine("Practice ideas");
        Console.WriteLine("--------------");
        Console.WriteLine("- Swap the app idea for something you want to build.");
        Console.WriteLine("- Change the preferred tags to simulate local vs cloud routing.");
        Console.WriteLine("- Copy the prompt into your preferred AI tool and compare the returned plan.");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}
