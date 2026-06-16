using System.Diagnostics;
using Aura.Infrastructure.Adapters.Ingestion.Embedding;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polly;

namespace Aura.UnitTests.Infrastructure;

public class MeaiEmbeddingProviderTests
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator =
        Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();

    private static EmbeddingProviderOptions DefaultOptions(int maxBatch = 16, int maxTokens = 8192) =>
        new()
        {
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "text-embedding-ada-002",
            MaxBatchSize = maxBatch,
            MaxTokensPerBatch = maxTokens
        };

    private MeaiEmbeddingProvider CreateProvider(
        EmbeddingProviderOptions? options = null,
        ResiliencePipeline? resiliencePipeline = null)
    {
        var opts = Options.Create(options ?? DefaultOptions());
        return new MeaiEmbeddingProvider(_generator, opts, resiliencePipeline ?? ResiliencePipeline.Empty);
    }

    private static GeneratedEmbeddings<Embedding<float>> CreateEmbeddings(params float[][] vectors)
    {
        var result = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var vec in vectors)
            result.Add(new Embedding<float>(vec));
        return result;
    }

    // ── Batch splitting by item count ──────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_SmallBatch_CallsGeneratorOnce()
    {
        var texts = new List<string> { "hello", "world" };
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(CreateEmbeddings([0.1f, 0.2f], [0.3f, 0.4f]));

        var provider = CreateProvider(DefaultOptions(maxBatch: 16));
        var result = await provider.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(0.1f, result[0].Span[0], 0.001f);
        Assert.Equal(0.3f, result[1].Span[0], 0.001f);
        await _generator.Received(1).GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ExceedsMaxBatchSize_SplitsIntoSubBatches()
    {
        var texts = new List<string> { "a", "b", "c", "d", "e" };

        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var input = callInfo.Arg<IEnumerable<string>>().ToList();
                return CreateEmbeddings(input.Select(_ => new float[] { 0.5f }).ToArray());
            });

        var provider = CreateProvider(DefaultOptions(maxBatch: 2));
        var result = await provider.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Equal(5, result.Count);
        // With maxBatch=2, 5 items → 3 calls (2, 2, 1)
        await _generator.Received(3).GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
    }

    // ── Batch splitting by token limit ──────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_ExceedsMaxTokensPerBatch_SplitsOnTokenLimit()
    {
        // Each text ~25 chars → ~6 estimated tokens (chars/4).
        // MaxTokensPerBatch=10 means each text forces a new sub-batch.
        var texts = new List<string> { "This is a longer sentence", "Another long text here too" };

        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var input = callInfo.Arg<IEnumerable<string>>().ToList();
                return CreateEmbeddings(input.Select(_ => new float[] { 0.1f }).ToArray());
            });

        var provider = CreateProvider(DefaultOptions(maxBatch: 100, maxTokens: 10));
        var result = await provider.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Equal(2, result.Count);
        // Each text exceeds 10 token limit individually, so processed one-per-batch
        await _generator.Received(2).GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
    }

    // ── Mapping from MEAI Embedding<float> to ReadOnlyMemory<float> ────

    [Fact]
    public async Task GenerateEmbeddingsAsync_MapsEmbeddingVectorCorrectly()
    {
        var expectedVector = new float[] { 0.11f, 0.22f, 0.33f };
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(CreateEmbeddings(expectedVector));

        var provider = CreateProvider();
        var result = await provider.GenerateEmbeddingsAsync(new List<string> { "test" }, CancellationToken.None);

        Assert.Equal(3, result[0].Length);
        Assert.Equal(0.11f, result[0].Span[0], 0.001f);
        Assert.Equal(0.22f, result[0].Span[1], 0.001f);
        Assert.Equal(0.33f, result[0].Span[2], 0.001f);
    }

    // ── Activity tag emission ──────────────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_SetsActivityTags_BatchSizeAndTokenUsage()
    {
        var texts = new List<string> { "hello", "world" };
        var embeddings = CreateEmbeddings([0.1f], [0.2f]);
        embeddings.Usage = new UsageDetails { InputTokenCount = 10, TotalTokenCount = 10 };

        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(embeddings);

        var capturedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Aura.Infrastructure.Embedding",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var provider = CreateProvider();
        await provider.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Single(capturedActivities);

        Assert.Equal(2, (int)capturedActivities[0].GetTagItem("batch_size")!);
        Assert.Equal((long)10, capturedActivities[0].GetTagItem("token_usage"));
        Assert.Equal("text-embedding-ada-002", capturedActivities[0].GetTagItem("model_name"));
    }

    // ── Edge cases ─────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_EmptyList_ReturnsEmpty()
    {
        var provider = CreateProvider();
        var result = await provider.GenerateEmbeddingsAsync(new List<string>(), CancellationToken.None);

        Assert.Empty(result);
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<EmbeddingGenerationOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_SingleItem_ReturnsOneEmbedding()
    {
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(CreateEmbeddings([0.5f, 0.6f]));

        var provider = CreateProvider();
        var result = await provider.GenerateEmbeddingsAsync(new List<string> { "single" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(0.5f, result[0].Span[0], 0.001f);
    }

    // ── Preserves order ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_PreservesInputOrder_AcrossSubBatches()
    {
        var texts = new List<string> { "first", "second", "third" };

        var callIndex = 0;
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var input = callInfo.Arg<IEnumerable<string>>().ToList();
                var result = CreateEmbeddings(
                    input.Select(_ => new float[] { ++callIndex * 0.1f }).ToArray());
                return result;
            });

        var provider = CreateProvider(DefaultOptions(maxBatch: 2));
        var result = await provider.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Equal(3, result.Count);
        // First sub-batch has 2 items, second has 1 — order must be preserved
        Assert.True(result[0].Span[0] < result[2].Span[0],
            "Results should maintain input order across sub-batches");
    }

    // ── Resilience pipeline integration ──────────────────────────────────

    [Fact]
    public async Task GenerateEmbeddingsAsync_TransientFailure_RetriesViaResiliencePipeline()
    {
        var callCount = 0;
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("429 Too Many Requests", null, System.Net.HttpStatusCode.TooManyRequests);
                return CreateEmbeddings([0.1f]);
            });

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(1),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();

        var provider = CreateProvider(resiliencePipeline: pipeline);
        var result = await provider.GenerateEmbeddingsAsync(new List<string> { "test" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, callCount); // 1 failure + 1 success
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_AllRetriesExhausted_Throws()
    {
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns<GeneratedEmbeddings<Embedding<float>>>(_ =>
                throw new HttpRequestException("503 Service Unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable));

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(1),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();

        var provider = CreateProvider(resiliencePipeline: pipeline);
        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GenerateEmbeddingsAsync(new List<string> { "test" }, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_NoResiliencePipeline_UsesEmpty()
    {
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(CreateEmbeddings([0.5f]));

        // ResiliencePipeline.Empty is the default — no retry, passes through
        var provider = CreateProvider();
        var result = await provider.GenerateEmbeddingsAsync(new List<string> { "test" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(0.5f, result[0].Span[0], 0.001f);
    }
}
