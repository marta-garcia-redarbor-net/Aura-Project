namespace Aura.Application.Models;

/// <summary>
/// DTO for priority-based counts and top-ranked items on the dashboard.
/// </summary>
public sealed record DashboardPriorityDto
{
    /// <summary>Number of items with Critical priority.</summary>
    public int CriticalCount { get; init; }

    /// <summary>Number of items with High priority.</summary>
    public int HighCount { get; init; }

    /// <summary>Number of items with Medium priority.</summary>
    public int MediumCount { get; init; }

    /// <summary>Number of items with Low priority.</summary>
    public int LowCount { get; init; }

    /// <summary>Top 3 highest-scored items for priority-focused display.</summary>
    public IReadOnlyList<InboxItemPreviewDto> TopItems { get; init; } = Array.Empty<InboxItemPreviewDto>();
}
