using AgentOrchestration.Core;

var nodes = new[]
{
    new AgentNode(
        Name: "code-lab",
        OperatingSystem: "Ubuntu",
        ExecutionMode: "local",
        IsAvailable: true,
        HasGpu: false,
        Priority: 1,
        Endpoint: "prompt-workshop",
        Tags: ["coding", "local", "beginner"]),
    new AgentNode(
        Name: "cloud-fallback",
        OperatingSystem: "Linux",
        ExecutionMode: "cloud",
        IsAvailable: true,
        HasGpu: false,
        Priority: 99,
        Endpoint: "https://cloud-agent.example.invalid",
        Tags: ["coding", "cloud", "fallback"])
};

var request = new AgentTaskRequest(
    Name: "Beginner coding lesson",
    Command:
    """
    Write the next coding task for a beginner todo-list console app.
    Include:
    1. the files to touch,
    2. the feature to add,
    3. the tests or manual checks to run,
    4. a short definition of done.
    """,
    PreferredTags: ["coding", "beginner", "local"]);

var dispatcher = new AgentDispatchService();
var runner = new CodingLessonRunner();
var result = await dispatcher.DispatchAsync(nodes, request, runner);

Console.WriteLine("AgentOrchestration.Beginner.Coding");
Console.WriteLine("==================================");
Console.WriteLine("Goal: learn how to ask an AI agent for a focused coding task after you already have a plan.");
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

sealed class CodingLessonRunner : IAgentTaskRunner
{
    public Task RunAsync(AgentNode node, AgentTaskRequest request, CancellationToken cancellationToken)
    {
        if (!node.Tags.Contains("coding", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Node '{node.Name}' is not configured for coding lessons.");
        }

        Console.WriteLine("Suggested coding prompt");
        Console.WriteLine("-----------------------");
        Console.WriteLine(request.Command);
        Console.WriteLine();
        Console.WriteLine("Practice ideas");
        Console.WriteLine("--------------");
        Console.WriteLine("- Ask for one small task at a time instead of a whole app.");
        Console.WriteLine("- Keep the definition of done explicit so the generated code stays focused.");
        Console.WriteLine("- After code is generated, review it and run the listed checks yourself.");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}
