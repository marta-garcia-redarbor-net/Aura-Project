using Aura.Domain.FocusState;

namespace Aura.Application.Ports;

/// <summary>
/// Port for persisting user-defined focus state overrides.
/// Overrides take precedence over the automatic focus state resolved by <see cref="IFocusStateResolver"/>.
/// </summary>
public interface IFocusStateOverrideStore
{
    /// <summary>
    /// Gets the active focus state override for the specified user, if one exists.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The overridden <see cref="FocusStateType"/>, or <c>null</c> if no override is set.</returns>
    Task<FocusStateType?> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a focus state override for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="state">The focus state to override with.</param>
    Task SetAsync(string userId, FocusStateType state);

    /// <summary>
    /// Clears the active focus state override for the specified user, returning to automatic resolution.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    Task ClearAsync(string userId);
}
