namespace AgentOrchestration.Core;

public sealed class AgentDispatchService
{
    public async Task<AgentDispatchResult> DispatchAsync(
        IEnumerable<AgentNode> nodes,
        AgentTaskRequest request,
        IAgentTaskRunner runner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(runner);

        var orderedNodes = nodes
            .Where(node => node.IsAvailable)
            .OrderByDescending(node => request.PreferredTags.Count(tag => node.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .ThenByDescending(node => node.HasGpu)
            .ThenBy(node => node.Priority)
            .ToList();

        var attempts = new List<AgentDispatchAttempt>();

        foreach (var node in orderedNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await runner.RunAsync(node, request, cancellationToken);
                attempts.Add(new AgentDispatchAttempt(node.Name, true, $"Executed '{request.Command}' on {node.Endpoint}."));
                return new AgentDispatchResult(true, node.Name, attempts);
            }
            catch (Exception exception)
            {
                attempts.Add(new AgentDispatchAttempt(node.Name, false, exception.Message));
            }
        }

        if (attempts.Count == 0)
        {
            attempts.Add(new AgentDispatchAttempt("<none>", false, "No available nodes matched the request."));
        }

        return new AgentDispatchResult(false, null, attempts);
    }
}
