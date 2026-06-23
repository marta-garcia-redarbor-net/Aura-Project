using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Composes Morning Summary payloads for a user/window request.
/// </summary>
public interface IMorningSummaryComposer
{
    /// <summary>
    /// Composes a summary payload.
    /// </summary>
    /// <param name="request">Summary composition input with user and resolved window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Composed Morning Summary payload.</returns>
    /// <remarks>
    /// Composition should be deterministic for the same request and underlying input set,
    /// enabling safe caching in future infrastructure adapters.
    /// </remarks>
    Task<MorningSummary> ComposeAsync(MorningSummaryRequest request, CancellationToken cancellationToken);
}
