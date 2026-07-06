namespace Aura.UI.Models;

/// <summary>
/// DTO for recent error entries displayed on the dashboard.
/// </summary>
public sealed record ErrorEntryDto(
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Message);
