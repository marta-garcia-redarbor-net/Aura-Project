using Aura.Application.Ports;
using Aura.Domain.FocusState;

namespace Aura.Application.Services;

/// <summary>
/// Default <see cref="IFocusStateResolver"/> that always returns <see cref="FocusStateType.WindowOfOpportunity"/>.
/// Placeholder until W3-H2 wires real signal sources (calendar, activity, preferences).
/// </summary>
public sealed class FocusStateResolver : IFocusStateResolver
{
    /// <inheritdoc />
    public Task<FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Placeholder: returns WindowOfOpportunity by default.
        // W3-H2 will wire real signal sources (calendar, activity, preferences).
        var state = new FocusState();
        return Task.FromResult(state);
    }
}
