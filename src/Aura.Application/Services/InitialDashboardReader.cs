using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

/// <summary>
/// Composes the initial dashboard payload from current-user context.
/// </summary>
public sealed class InitialDashboardReader : IInitialDashboardReader
{
    private readonly ICurrentUserService _currentUserService;

    public InitialDashboardReader(ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);
        _currentUserService = currentUserService;
    }

    public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentUser = _currentUserService.GetCurrentUser();
        var userDisplayName = Normalize(currentUser?.DisplayName);
        var cards = BuildCards(currentUser).ToArray();

        return Task.FromResult(new InitialDashboardDto(userDisplayName, cards));
    }

    private static IEnumerable<DashboardCardDto> BuildCards(AuraUser? currentUser)
    {
        if (currentUser is null)
        {
            yield break;
        }

        foreach (var card in CreateCards(currentUser))
        {
            if (card is not null)
            {
                yield return card;
            }
        }
    }

    private static DashboardCardDto?[] CreateCards(AuraUser currentUser) =>
    [
        CreateCard("Signed in as", currentUser.DisplayName, "info"),
        CreateCard("Email", currentUser.Email, "ready")
    ];

    private static DashboardCardDto? CreateCard(string title, string? value, string status)
    {
        var normalizedValue = Normalize(value);
        return string.IsNullOrEmpty(normalizedValue)
            ? null
            : new DashboardCardDto(title, normalizedValue, status);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
