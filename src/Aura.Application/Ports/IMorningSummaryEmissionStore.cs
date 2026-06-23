namespace Aura.Application.Ports;

public interface IMorningSummaryEmissionStore
{
    Task<bool> HasBeenEmittedAsync(string userId, DateOnly localDate, CancellationToken ct);

    Task MarkEmittedAsync(string userId, DateOnly localDate, CancellationToken ct);

    Task ResetAsync(string userId, DateOnly localDate, CancellationToken ct);
}
