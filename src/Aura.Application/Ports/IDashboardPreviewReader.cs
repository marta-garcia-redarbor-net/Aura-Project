using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Reads the dashboard preview payload for the current user.
/// </summary>
public interface IDashboardPreviewReader
{
    /// <summary>
    /// Returns inbox-by-source and morning summary preview DTOs.
    /// </summary>
    Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken);
}
