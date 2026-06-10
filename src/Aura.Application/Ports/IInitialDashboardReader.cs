using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Reads the initial dashboard payload for the current user.
/// </summary>
public interface IInitialDashboardReader
{
    /// <summary>
    /// Returns the initial dashboard DTO.
    /// </summary>
    Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken);
}
