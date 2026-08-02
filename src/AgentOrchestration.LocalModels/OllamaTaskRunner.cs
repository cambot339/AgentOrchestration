using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AgentOrchestration.Core;

namespace AgentOrchestration.LocalModels;

public sealed class OllamaTaskRunner(HttpClient httpClient) : IAgentTaskRunner
{
    public async Task RunAsync(
        AgentNode node,
        AgentTaskRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = node.Endpoint.TrimEnd('/');
        var response = await httpClient.PostAsJsonAsync(
            $"{endpoint}/api/generate",
            new OllamaGenerateRequest(node.ExecutionMode, request.Command, Stream: false),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Ollama returned {(int)response.StatusCode} {response.ReasonPhrase}: {details}");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result?.Response))
        {
            throw new InvalidOperationException("Ollama returned an empty response.");
        }

        Console.WriteLine(result.Response.Trim());
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);
}
