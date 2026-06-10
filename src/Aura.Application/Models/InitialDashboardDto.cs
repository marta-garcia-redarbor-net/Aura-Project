namespace Aura.Application.Models;

/// <summary>
/// API-facing DTO for the first dashboard slice.
/// </summary>
/// <param name="UserDisplayName">Current user display name.</param>
/// <param name="Cards">Initial summary cards for the dashboard shell.</param>
public sealed record InitialDashboardDto(string UserDisplayName, IReadOnlyList<DashboardCardDto> Cards);

/// <summary>
/// Small dashboard card payload consumed by the API/UI contract.
/// </summary>
/// <param name="Title">Card title.</param>
/// <param name="Value">Card value.</param>
/// <param name="Status">Card visual status hint.</param>
public sealed record DashboardCardDto(string Title, string Value, string Status);
