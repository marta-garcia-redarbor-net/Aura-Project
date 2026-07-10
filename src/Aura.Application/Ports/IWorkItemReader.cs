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
    /// Reads work items for the provided query window, optionally filtered by status.
    /// When statusFilter is null, returns all statuses.
    /// </summary>
    Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Reads work items for the provided query window, optionally filtered by source type and status.
    /// </summary>
    Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        WorkItemSourceType sourceType,
        MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Reads work items filtered by source type, optionally filtered by status and owner.
    /// When ownerUserId is null, returns all items regardless of owner.
    /// When ownerUserId is provided, returns items where OwnerUserId is null (shared/seed) or matches.
    /// </summary>
    Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
        WorkItemSourceType sourceType, WorkItemStatus? statusFilter, string? ownerUserId, CancellationToken cancellationToken);
}
