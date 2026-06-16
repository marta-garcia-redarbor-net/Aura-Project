using System.Diagnostics;
using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly;

namespace Aura.Infrastructure.Adapters.Ingestion.Embedding;

/// <summary>
/// Infrastructure adapter wrapping MEAI's <see cref="IEmbeddingGenerator{String, Embedding}"/>
/// to implement the Application-layer <see cref="IEmbeddingProvider"/> port.
/// <para>
/// Responsibilities:
/// - Batch-splitting by <see cref="EmbeddingProviderOptions.MaxBatchSize"/> (item count)
///   and <see cref="EmbeddingProviderOptions.MaxTokensPerBatch"/> (estimated token limit).
/// - Mapping from MEAI <see cref="Embedding{T}"/> to <see cref="ReadOnlyMemory{T}"/>.
/// - Emitting custom Activity tags for observability (batch_size, token_usage, model_name).
/// - Resilience via Polly <see cref="ResiliencePipeline"/> (retry, timeout).
/// </para>
/// </summary>
internal sealed class MeaiEmbeddingProvider : IEmbeddingProvider
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly EmbeddingProviderOptions _options;
    private readonly ResiliencePipeline _resiliencePipeline;

    private static readonly ActivitySource ActivitySourceInstance =
        new("Aura.Infrastructure.Embedding", "1.0.0");

    public MeaiEmbeddingProvider(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        IOptions<EmbeddingProviderOptions> options,
        ResiliencePipeline resiliencePipeline)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _resiliencePipeline = resiliencePipeline ?? throw new ArgumentNullException(nameof(resiliencePipeline));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
            return Array.Empty<ReadOnlyMemory<float>>();

        var subBatches = SplitIntoBatches(texts);
        var allResults = new List<ReadOnlyMemory<float>>(texts.Count);
        var totalTokenUsage = 0L;

        foreach (var batch in subBatches)
        {
            using var activity = ActivitySourceInstance.StartActivity("GenerateEmbeddings");
            activity?.SetTag("batch_size", batch.Count);
            activity?.SetTag("model_name", _options.DeploymentName);

            var generated = await _resiliencePipeline.ExecuteAsync(
                async token => await _generator.GenerateAsync(batch, cancellationToken: token),
                ct);

            if (generated.Usage is not null)
            {
                totalTokenUsage += generated.Usage.TotalTokenCount ?? 0;
                activity?.SetTag("token_usage", generated.Usage.TotalTokenCount);
            }

            foreach (var embedding in generated)
            {
                allResults.Add(embedding.Vector);
            }
        }

        return allResults;
    }

    /// <summary>
    /// Splits input texts into sub-batches respecting both item count and estimated token limits.
    /// Token estimation uses chars/4 as a rough approximation of GPT tokenization.
    /// </summary>
    internal IReadOnlyList<List<string>> SplitIntoBatches(IReadOnlyList<string> texts)
    {
        var batches = new List<List<string>>();
        var currentBatch = new List<string>();
        var currentTokens = 0;

        foreach (var text in texts)
        {
            var estimatedTokens = EstimateTokens(text);

            var wouldExceedItemLimit = currentBatch.Count >= _options.MaxBatchSize;
            var wouldExceedTokenLimit = currentBatch.Count > 0
                && currentTokens + estimatedTokens > _options.MaxTokensPerBatch;

            if (wouldExceedItemLimit || wouldExceedTokenLimit)
            {
                batches.Add(currentBatch);
                currentBatch = new List<string>();
                currentTokens = 0;
            }

            currentBatch.Add(text);
            currentTokens += estimatedTokens;
        }

        if (currentBatch.Count > 0)
            batches.Add(currentBatch);

        return batches;
    }

    /// <summary>
    /// Rough token estimation: chars / 4.
    /// Conservative enough for batch-splitting purposes; over-estimating is safer than under.
    /// </summary>
    internal static int EstimateTokens(string text)
        => Math.Max(1, (text.Length + 3) / 4); // ceiling division
}
