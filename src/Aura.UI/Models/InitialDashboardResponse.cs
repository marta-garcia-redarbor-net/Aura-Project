namespace Aura.UI.Models;

public sealed record InitialDashboardResponse(
    string UserDisplayName,
    IReadOnlyList<DashboardCardResponse> Cards);

public sealed record DashboardCardResponse(
    string Title,
    string Value,
    string Status);
