using System.Net;
using Aura.Application.Ports;
using Aura.Infrastructure.Embedding;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;
using Polly.Timeout;

namespace Aura.IntegrationTests.Embedding;

/// <summary>
/// Integration tests for embedding provider resilience.
/// Validates Polly retry/timeout pipelines work end-to-end through real DI wiring.
/// Uses a fake IEmbeddingGenerator that simulates transient HttpRequestException failures.
/// </summary>
public class EmbeddingResilienceTests
{
    /// <summary>
    /// Verifies that a transient 429 error is retried and the second attempt succeeds.
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_Transient429_RetriesAndSucceeds()
    {
        var fakeGenerator = new TransientFailureGenerator(
            HttpStatusCode.TooManyRequests, succeedOnAttempt: 2);
        var provider = BuildProviderWithFakeGenerator(fakeGenerator, maxRetries: 3);

        var result = await provider.GenerateEmbeddingsAsync(
            new[] { "resilience test" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(3, result[0].Length);
        Assert.Equal(2, fakeGenerator.AttemptCount);
    }

    /// <summary>
    /// Verifies that 503 Service Unavailable is also retried (multiple retryable codes).
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_Transient503_RetriesAndSucceeds()
    {
        var fakeGenerator = new TransientFailureGenerator(
            HttpStatusCode.ServiceUnavailable, succeedOnAttempt: 3);
        var provider = BuildProviderWithFakeGenerator(fakeGenerator, maxRetries: 3);

        var result = await provider.GenerateEmbeddingsAsync(
            new[] { "503 test" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(3, fakeGenerator.AttemptCount);
    }

    /// <summary>
    /// Verifies that when all retries are exhausted on transient errors, the exception surfaces.
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_AllRetriesExhausted_ThrowsHttpRequestException()
    {
        var fakeGenerator = new TransientFailureGenerator(
            HttpStatusCode.TooManyRequests, succeedOnAttempt: int.MaxValue);
        var provider = BuildProviderWithFakeGenerator(fakeGenerator, maxRetries: 2);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GenerateEmbeddingsAsync(
                new[] { "will exhaust retries" }, CancellationToken.None));

        // 1 initial + 2 retries = 3 attempts
        Assert.Equal(3, fakeGenerator.AttemptCount);
    }

    /// <summary>
    /// Verifies that non-retryable status codes (e.g. 400) are NOT retried by Polly.
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_NonRetryable400_DoesNotRetry()
    {
        var fakeGenerator = new TransientFailureGenerator(
            HttpStatusCode.BadRequest, succeedOnAttempt: int.MaxValue);
        var provider = BuildProviderWithFakeGenerator(fakeGenerator, maxRetries: 3);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GenerateEmbeddingsAsync(
                new[] { "bad request" }, CancellationToken.None));

        Assert.Equal(1, fakeGenerator.AttemptCount);
    }

    /// <summary>
    /// Verifies that when the embedding generator stalls beyond the configured timeout,
    /// Polly's timeout policy fires and a TimeoutRejectedException is thrown.
    /// Uses a never-completing TaskCompletionSource for deterministic behavior.
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_TimeoutExceeded_ThrowsTimeoutRejectedException()
    {
        var stallingGenerator = new StallingGenerator();
        var provider = BuildProviderWithFakeGenerator(stallingGenerator, timeoutSeconds: 1);

        await Assert.ThrowsAsync<TimeoutRejectedException>(
            () => provider.GenerateEmbeddingsAsync(
                new[] { "this will stall forever" }, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that when the generator completes within the timeout, no exception is thrown.
    /// Triangulation against the timeout test above.
    /// </summary>
    [Fact]
    public async Task GenerateEmbeddingsAsync_CompletesWithinTimeout_Succeeds()
    {
        var fakeGenerator = new TransientFailureGenerator(
            HttpStatusCode.TooManyRequests, succeedOnAttempt: 1);
        var provider = BuildProviderWithFakeGenerator(fakeGenerator, maxRetries: 1, timeoutSeconds: 30);

        var result = await provider.GenerateEmbeddingsAsync(
            new[] { "fast response" }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(3, result[0].Length);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static IEmbeddingProvider BuildProviderWithFakeGenerator(
        IEmbeddingGenerator<string, Embedding<float>> fakeGenerator,
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProvider:Endpoint"] = "https://fake.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key",
                ["EmbeddingProvider:MaxRetries"] = maxRetries.ToString(),
                ["EmbeddingProvider:TimeoutSeconds"] = timeoutSeconds.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMeaiEmbeddingProvider(config);

        // Replace the MEAI generator with our fake
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEmbeddingGenerator<string, Embedding<float>>));
        if (descriptor != null) services.Remove(descriptor);
        services.AddSingleton(fakeGenerator);

        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IEmbeddingProvider>();
    }

    /// <summary>
    /// Fake IEmbeddingGenerator that throws HttpRequestException with configurable status codes
    /// until a specified attempt number, then returns valid embeddings.
    /// </summary>
    private sealed class TransientFailureGenerator
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly HttpStatusCode _failStatusCode;
        private readonly int _succeedOnAttempt;
        private int _attemptCount;

        public int AttemptCount => _attemptCount;

        public TransientFailureGenerator(HttpStatusCode failStatusCode, int succeedOnAttempt)
        {
            _failStatusCode = failStatusCode;
            _succeedOnAttempt = succeedOnAttempt;
        }

        public EmbeddingGeneratorMetadata Metadata { get; } =
            new("test-model");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _attemptCount);

            if (_attemptCount < _succeedOnAttempt)
            {
                throw new HttpRequestException(
                    $"Simulated {(int)_failStatusCode}",
                    null,
                    _failStatusCode);
            }

            var result = new GeneratedEmbeddings<Embedding<float>>
            {
                Usage = new UsageDetails { TotalTokenCount = 5 }
            };
            foreach (var _ in values)
            {
                result.Add(new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f }));
            }
            return Task.FromResult(result);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => null;

        public void Dispose() { }
    }

    /// <summary>
    /// Fake IEmbeddingGenerator that never completes — uses a TaskCompletionSource
    /// to deterministically stall until cancelled by the Polly timeout policy.
    /// </summary>
    private sealed class StallingGenerator
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } =
            new("stalling-model");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Never-completing task — Polly timeout will cancel this
            var tcs = new TaskCompletionSource<GeneratedEmbeddings<Embedding<float>>>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => null;

        public void Dispose() { }
    }
}
