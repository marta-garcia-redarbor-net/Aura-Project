using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Applies deterministic ranking policy for Morning Summary work items.
/// </summary>
public interface IMorningSummaryRankingPolicy
{
    /// <summary>
    /// Ranks input items in deterministic order with per-item explanation.
    /// </summary>
    IReadOnlyList<RankedWorkItem> Rank(IReadOnlyList<WorkItem> items);
}
