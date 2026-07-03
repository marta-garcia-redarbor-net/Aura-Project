using Aura.Domain.FocusState;

namespace Aura.Application.Ports;

/// <summary>
/// Port for resolving the current focus state for a given user.
/// Implementations may consume calendar, activity, and preference signals.
/// </summary>
public interface IFocusStateResolver
{
    /// <summary>
    /// Resolves the current <see cref="FocusState"/> for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved <see cref="FocusState"/>.</returns>
    Task<FocusState> ResolveAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
