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
}
