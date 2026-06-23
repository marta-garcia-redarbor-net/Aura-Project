using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IMorningSummaryScheduler
{
    Task<MorningSummaryDueState> ResolveAsync(string userId, CancellationToken ct);
}
