using Aura.Infrastructure.VectorStore;

namespace Aura.UnitTests.VectorStore;

public class AzureOpenAiEmbeddingProviderTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_NullText_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.GenerateEmbeddingAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ThrowsArgumentException()
    {
        var provider = CreateProvider();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.GenerateEmbeddingAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ReturnsNonEmptyEmbedding()
    {
        // Arrange: mock handler returns valid embedding response
        var handler = new FakeHttpHandler("""
            {
              "data": [{ "embedding": [0.1, 0.2, 0.3], "index": 0 }],
              "model": "text-embedding-ada-002",
              "usage": { "prompt_tokens": 5, "total_tokens": 5 }
            }
            """);
        var provider = CreateProvider(handler);

        // Act
        var embedding = await provider.GenerateEmbeddingAsync("Hello world", CancellationToken.None);

        // Assert
        Assert.Equal(3, embedding.Length);
        Assert.Equal(0.1f, embedding.Span[0], 0.001f);
        Assert.Equal(0.2f, embedding.Span[1], 0.001f);
        Assert.Equal(0.3f, embedding.Span[2], 0.001f);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_DifferentTexts_ReturnsDifferentEmbeddings()
    {
        var handler1 = new FakeHttpHandler("""{"data":[{"embedding":[0.1,0.2],"index":0}],"model":"m","usage":{"prompt_tokens":1,"total_tokens":1}}""");
        var handler2 = new FakeHttpHandler("""{"data":[{"embedding":[0.9,0.8],"index":0}],"model":"m","usage":{"prompt_tokens":1,"total_tokens":1}}""");

        var provider1 = CreateProvider(handler1);
        var provider2 = CreateProvider(handler2);

        var emb1 = await provider1.GenerateEmbeddingAsync("Hello", CancellationToken.None);
        var emb2 = await provider2.GenerateEmbeddingAsync("World", CancellationToken.None);

        // They should be different embeddings
        Assert.NotEqual(emb1.Span[0], emb2.Span[0]);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ApiError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(statusCode: System.Net.HttpStatusCode.InternalServerError);
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateEmbeddingAsync("test", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SendsCorrectRequestBody()
    {
        var handler = new FakeHttpHandler("""{"data":[{"embedding":[0.5],"index":0}],"model":"m","usage":{"prompt_tokens":1,"total_tokens":1}}""");
        var provider = CreateProvider(handler);

        await provider.GenerateEmbeddingAsync("My test text", CancellationToken.None);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("My test text", handler.LastRequestBody);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AzureOpenAiEmbeddingProvider CreateProvider(FakeHttpHandler? handler = null)
    {
        handler ??= new FakeHttpHandler("""{"data":[{"embedding":[0.1],"index":0}],"model":"m","usage":{"prompt_tokens":1,"total_tokens":1}}""");
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.openai.azure.com/")
        };
        return new AzureOpenAiEmbeddingProvider(httpClient, "test-deployment", "2024-02-01");
    }

    /// <summary>
    /// Fake HTTP handler that returns a predetermined response for unit testing.
    /// </summary>
    internal sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string? _responseBody;
        private readonly System.Net.HttpStatusCode _statusCode;

        public string? LastRequestBody { get; private set; }

        public FakeHttpHandler(string responseBody, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        public FakeHttpHandler(System.Net.HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            _responseBody = null;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(_statusCode);
            if (_responseBody is not null)
                response.Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json");
            return response;
        }
    }
}
