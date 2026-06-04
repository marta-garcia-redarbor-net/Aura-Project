using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Application.Ports;

namespace Aura.Infrastructure.VectorStore;

/// <summary>
/// Minimal V1 Azure OpenAI embedding provider using raw HTTP calls.
/// <para>
/// WARNING: This is a minimal implementation intended to bootstrap the semantic index pipeline.
/// It requires future hardening including:
/// - Retry policies (Polly / resilience pipeline)
/// - Rate limiting / throttling awareness
/// - Token counting and input truncation
/// - Configurable timeouts
/// - Telemetry / observability instrumentation
/// - Support for batch embedding requests
/// - Error classification (transient vs permanent)
/// </para>
/// </summary>
/// <remarks>
/// TODO: Replace with a resilient, production-grade implementation before scaling.
/// The Azure OpenAI SDK (<c>Azure.AI.OpenAI</c>) or the official <c>OpenAI</c> .NET client
/// should be evaluated for V2 to gain automatic retries, token management, and type safety.
/// </remarks>
public sealed class AzureOpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _deploymentName;
    private readonly string _apiVersion;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AzureOpenAiEmbeddingProvider(
        HttpClient httpClient,
        string deploymentName,
        string apiVersion)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
        _apiVersion = apiVersion ?? throw new ArgumentNullException(nameof(apiVersion));
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text must not be null or empty.", nameof(text));

        var requestUri = $"openai/deployments/{_deploymentName}/embeddings?api-version={_apiVersion}";

        var requestBody = new EmbeddingRequest { Input = text };
        var response = await _httpClient.PostAsJsonAsync(requestUri, requestBody, JsonOptions, ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Azure OpenAI returned null embedding response.");

        if (result.Data is null || result.Data.Count == 0)
            throw new InvalidOperationException("Azure OpenAI returned empty embedding data.");

        return new ReadOnlyMemory<float>(result.Data[0].Embedding);
    }

    // ── Request / Response models (internal, JSON-serializable) ──────

    private sealed class EmbeddingRequest
    {
        public string Input { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        public List<EmbeddingData>? Data { get; set; }
    }

    private sealed class EmbeddingData
    {
        public float[] Embedding { get; set; } = [];
        public int Index { get; set; }
    }
}
