using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;

namespace Aura.Infrastructure.Embedding;

/// <summary>
/// Polly resilience pipeline factory for embedding provider operations.
/// Centralizes retry and timeout strategies for OpenAI/MEAI calls.
/// </summary>
public static class EmbeddingResiliencePolicyBuilder
{
    /// <summary>
    /// Key for the resilience pipeline in the Polly registry.
    /// </summary>
    public const string PipelineKey = "EmbeddingProvider";

    /// <summary>
    /// Registers the embedding provider resilience pipeline with the DI container.
    /// Applies exponential backoff retry (transient HTTP 429/503/timeout) + timeout.
    /// </summary>
    /// <remarks>
    /// Strategy:
    /// - Retry on 429 (TooManyRequests), 503 (ServiceUnavailable), timeout
    /// - Exponential backoff: base 1s, randomized jitter
    /// - Max retries and timeout read from <see cref="EmbeddingProviderOptions"/>
    /// </remarks>
    public static void AddEmbeddingResiliencePolicy(
        this IServiceCollection services,
        EmbeddingProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddResiliencePipeline(PipelineKey, (builder, _) =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetries,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromSeconds(1),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>(ex =>
                            ex.StatusCode is HttpStatusCode.TooManyRequests
                                          or HttpStatusCode.ServiceUnavailable
                                          or HttpStatusCode.GatewayTimeout)
                })
                .AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
        });
    }
}
