namespace Aura.Application.Models;

/// <summary>
/// API-facing DTO for the first dashboard slice.
/// </summary>
/// <param name="UserDisplayName">Current user display name.</param>
/// <param name="Cards">Initial summary cards for the dashboard shell.</param>
public sealed record InitialDashboardDto(string UserDisplayName, IReadOnlyList<DashboardCardDto> Cards)
{
    /// <summary>Current user email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Total count of pending items.</summary>
    public int TotalPendingCount { get; init; }

    /// <summary>Count of pending items considered high priority (effective score &gt;= 75).</summary>
    public int HighPriorityCount { get; init; }

    /// <summary>Top 3 highest-priority items by PriorityScore DESC.</summary>
    public IReadOnlyList<InboxItemPreviewDto> TopItems { get; init; } = Array.Empty<InboxItemPreviewDto>();
}

/// <summary>
/// Small dashboard card payload consumed by the API/UI contract.
/// </summary>
/// <param name="Title">Card title.</param>
/// <param name="Value">Card value.</param>
/// <param name="Status">Card visual status hint.</param>
public sealed record DashboardCardDto(string Title, string Value, string Status);
