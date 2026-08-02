namespace AgentOrchestration.Core;

public sealed record AgentNode(
    string Name,
    string OperatingSystem,
    string ExecutionMode,
    bool IsAvailable,
    bool HasGpu,
    int Priority,
    string Endpoint,
    IReadOnlyList<string> Tags);

public sealed record AgentTaskRequest(
    string Name,
    string Command,
    IReadOnlyList<string> PreferredTags);

public sealed record AgentDispatchAttempt(
    string NodeName,
    bool Success,
    string Message);

public sealed record AgentDispatchResult(
    bool Success,
    string? NodeName,
    IReadOnlyList<AgentDispatchAttempt> Attempts);

public interface IAgentTaskRunner
{
    Task RunAsync(AgentNode node, AgentTaskRequest request, CancellationToken cancellationToken);
}
