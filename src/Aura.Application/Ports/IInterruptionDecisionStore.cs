using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for persisting and querying interruption decisions made by the policy engine.
/// All verdicts (INTERRUPT, QUEUE, DEFER) are recorded for audit.
/// </summary>
public interface IInterruptionDecisionStore
{
    /// <summary>
    /// Records a single interruption decision.
    /// </summary>
    /// <param name="record">The decision record to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries paginated decision history, sorted by timestamp descending.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result set of decision records.</returns>
    Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
