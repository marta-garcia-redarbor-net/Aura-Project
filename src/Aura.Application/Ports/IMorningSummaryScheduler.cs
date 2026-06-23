using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Resolves and evaluates Morning Summary windows.
/// </summary>
public interface IMorningSummaryScheduler
{
    Task<MorningSummaryDueState> ResolveAsync(string userId, CancellationToken ct);
}
