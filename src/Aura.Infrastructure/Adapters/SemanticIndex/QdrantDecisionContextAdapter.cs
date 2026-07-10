using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

internal sealed class QdrantDecisionContextAdapter : IDecisionContextRetriever
{
    private readonly ISemanticContextRetriever _semanticContextRetriever;
    private readonly ILogger<QdrantDecisionContextAdapter> _logger;

    public QdrantDecisionContextAdapter(
        ISemanticContextRetriever semanticContextRetriever,
        ILogger<QdrantDecisionContextAdapter> logger)
    {
        _semanticContextRetriever = semanticContextRetriever ?? throw new ArgumentNullException(nameof(semanticContextRetriever));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var query = new SemanticQuery
            {
                Text = string.IsNullOrWhiteSpace(item.Title) ? item.ExternalId : item.Title,
                TopK = 3
            };

            var chunks = await _semanticContextRetriever.RetrieveAsync(query, timeoutCts.Token);
            return chunks
                .Select(c => new DecisionContextItem(
                    CanonicalSourceId: c.Chunk.CanonicalSourceId,
                    ContentSnippet: c.Chunk.Content,
                    SourceType: c.Chunk.Collection.ToString(),
                    RelevanceScore: c.Score))
                .ToArray();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Decision context retrieval timed out. Falling back to empty context.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Decision context retrieval failed. Falling back to empty context.");
            return [];
        }
    }
}
