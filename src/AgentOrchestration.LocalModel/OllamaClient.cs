using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgentOrchestration.LocalModel;

// OllamaClient wraps the small set of Ollama REST endpoints you need to get
// started.  Ollama runs a local HTTP server (default: http://localhost:11434)
// that accepts JSON requests — no SDK required, just HttpClient.
//
// API reference: https://github.com/ollama/ollama/blob/main/docs/api.md
public sealed class OllamaClient
{
    private readonly HttpClient _http;

    public OllamaClient(string baseUrl = "http://localhost:11434")
    {
        // HttpClient is the standard .NET way to make HTTP requests.
        // BaseAddress lets us write relative paths in every call below.
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // -----------------------------------------------------------------------
    // IsRunningAsync
    // -----------------------------------------------------------------------
    // Ollama responds to GET / with the plain text "Ollama is running".
    // This is the cheapest way to confirm the process is up before sending
    // a real prompt.
    public async Task<bool> IsRunningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            // The server is not reachable — Ollama is probably not started.
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // ListModelsAsync
    // -----------------------------------------------------------------------
    // GET /api/tags returns every model that has been pulled with
    // `ollama pull <model>`.  You need at least one model before you can
    // generate text.
    public async Task<IReadOnlyList<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", cancellationToken);
        return result?.Models ?? [];
    }

    // -----------------------------------------------------------------------
    // GenerateAsync
    // -----------------------------------------------------------------------
    // POST /api/generate is the core inference endpoint.
    // Setting stream:false makes Ollama wait until the full response is
    // ready before sending JSON back.  That keeps the code simple while
    // you are learning — streaming can come later.
    public async Task<string> GenerateAsync(
        string model,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new OllamaGenerateRequest(model, prompt, Stream: false);

        using var response = await _http.PostAsJsonAsync("/api/generate", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);
        return result?.Response ?? string.Empty;
    }
}

// ---------------------------------------------------------------------------
// Request / response shapes
// ---------------------------------------------------------------------------
// These record types match the JSON fields that Ollama sends and expects.
// JsonPropertyName attributes map C# PascalCase names to the snake_case
// names used by the API.

public sealed record OllamaGenerateRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stream")] bool Stream);

public sealed record OllamaGenerateResponse(
    [property: JsonPropertyName("response")] string Response,
    [property: JsonPropertyName("done")] bool Done);

public sealed record OllamaTagsResponse(
    [property: JsonPropertyName("models")] IReadOnlyList<OllamaModelInfo> Models);

public sealed record OllamaModelInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size);
