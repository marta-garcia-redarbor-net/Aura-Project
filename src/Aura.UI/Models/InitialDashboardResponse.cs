namespace Aura.UI.Models;

public sealed record InitialDashboardResponse(
    string UserDisplayName,
    IReadOnlyList<DashboardCardResponse> Cards)
{
    public string Email { get; init; } = string.Empty;
}

public sealed record DashboardCardResponse(
    string Title,
    string Value,
    string Status);
