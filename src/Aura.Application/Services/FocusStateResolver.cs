using Aura.Application.Ports;
using Aura.Domain.FocusState;

namespace Aura.Application.Services;

/// <summary>
/// Resolves the current focus state for a user.
/// Checks for an explicit override in <see cref="IFocusStateOverrideStore"/> first;
/// if none is set, falls back to automatic resolution from signal sources.
/// </summary>
public sealed class FocusStateResolver : IFocusStateResolver
{
    private readonly IFocusStateOverrideStore _overrideStore;

    public FocusStateResolver(IFocusStateOverrideStore overrideStore)
    {
        _overrideStore = overrideStore ?? throw new ArgumentNullException(nameof(overrideStore));
    }

    /// <inheritdoc />
    public async Task<FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check for explicit user override first
        var overrideState = await _overrideStore.GetAsync(userId, cancellationToken);
        if (overrideState.HasValue)
        {
            return new FocusState(overrideState.Value);
        }

        // No override — auto-compute from signal sources (calendar, activity, preferences)
        // Placeholder: returns WindowOfOpportunity by default.
        // W3-H2 will wire real signal sources.
        var state = new FocusState();
        return state;
    }
}
