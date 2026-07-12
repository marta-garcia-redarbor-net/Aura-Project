namespace Aura.UI.Models;

public enum DashboardViewStateKind
{
    Loading,
    Empty,
    Error,
    Populated
}

public sealed record DashboardViewState(
    DashboardViewStateKind Kind,
    string UserDisplayName,
    IReadOnlyList<DashboardCardResponse> Cards,
    string Message)
{
    public string Email { get; init; } = string.Empty;

    public static DashboardViewState Loading()
        => new(
            DashboardViewStateKind.Loading,
            string.Empty,
            [],
            "Loading the first dashboard slice from Aura.Api.");
}

public static class DashboardViewStateMapper
{
    public static DashboardViewState FromResponse(InitialDashboardResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Cards.Count == 0
            ? new DashboardViewState(
                DashboardViewStateKind.Empty,
                response.UserDisplayName,
                [],
                "No dashboard items are available yet.")
            {
                Email = response.Email
            }
            : new DashboardViewState(
                DashboardViewStateKind.Populated,
                response.UserDisplayName,
                response.Cards,
                "Your initial dashboard summary is ready.")
            {
                Email = response.Email
            };
    }

    public static DashboardViewState FromError(Exception ex)
        => new(
            DashboardViewStateKind.Error,
            ex.Message,
            [],
            "We couldn't load the dashboard from Aura.Api. Please retry.");
}
