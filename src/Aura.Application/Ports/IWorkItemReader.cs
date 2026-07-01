using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Reads work items for a Morning Summary query window.
/// </summary>
public interface IWorkItemReader
{
    /// <summary>
    /// Reads work items for the provided query window.
    /// </summary>
    /// <param name="query">User and UTC window bounds used for work-item retrieval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of work items for the requested window.</returns>
    Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Reads work items filtered by source type and optional status.
    /// </summary>
    /// <param name="sourceType">Source type filter (TeamsMessage, OutlookEmail, etc.).</param>
    /// <param name="statusFilter">Optional status filter. When null, returns all statuses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of matching work items, sorted by priority then capture date descending.</returns>
    Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
        WorkItemSourceType sourceType,
        WorkItemStatus? statusFilter,
        CancellationToken cancellationToken);
}
