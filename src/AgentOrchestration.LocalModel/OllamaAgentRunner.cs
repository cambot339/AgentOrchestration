using AgentOrchestration.Core;
using AgentOrchestration.LocalModel;

namespace AgentOrchestration.LocalModel;

// OllamaAgentRunner implements the IAgentTaskRunner interface from
// AgentOrchestration.Core.  This is the bridge that lets the main
// AgentDispatchService send a task to Ollama on any node.
//
// How it works:
//   1. The dispatcher selects a node (e.g. "main-4070" at localhost).
//   2. It calls RunAsync with that node and the task request.
//   3. This runner builds an OllamaClient pointed at node.Endpoint and
//      sends the task command as a prompt.
//
// You can drop this runner into AgentOrchestration.App by passing an
// instance to dispatcher.DispatchAsync instead of DemoAgentTaskRunner.
public sealed class OllamaAgentRunner : IAgentTaskRunner
{
    // The name of the Ollama model to use when no model is specified in the
    // task command.  Override this to try different models.
    private readonly string _defaultModel;

    public OllamaAgentRunner(string defaultModel = "llama3.1:8b")
    {
        _defaultModel = defaultModel;
    }

    public async Task RunAsync(
        AgentNode node,
        AgentTaskRequest request,
        CancellationToken cancellationToken)
    {
        // Build the base URL from the node's endpoint.
        // Nodes with execution mode "local" are expected to have Ollama
        // reachable at port 11434 on their endpoint address.
        var baseUrl = node.ExecutionMode == "local"
            ? $"http://{node.Endpoint}:11434"
            : node.Endpoint;

        var client = new OllamaClient(baseUrl);

        // Confirm Ollama is actually running before attempting inference.
        var isUp = await client.IsRunningAsync(cancellationToken);
        if (!isUp)
        {
            throw new InvalidOperationException(
                $"Ollama is not reachable at {baseUrl}. " +
                "Start it with 'ollama serve' or install it from https://ollama.com.");
        }

        // Use the task command as the prompt so the dispatcher can pass
        // any natural language instruction through the normal request flow.
        var response = await client.GenerateAsync(_defaultModel, request.Command, cancellationToken);

        Console.WriteLine($"[OllamaAgentRunner] Response from {node.Name}:");
        Console.WriteLine(response);
    }
}
