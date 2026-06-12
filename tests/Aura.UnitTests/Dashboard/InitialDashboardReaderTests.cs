using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class InitialDashboardReaderTests
{
    [Fact]
    public async Task GetAsync_WithDisplayNameAndEmail_ReturnsPopulatedDashboard()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com"
        });

        IInitialDashboardReader reader = new InitialDashboardReader(currentUserService);

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.Equal("Test User", result.UserDisplayName);
        Assert.Collection(
            result.Cards,
            card =>
            {
                Assert.Equal("Signed in as", card.Title);
                Assert.Equal("Test User", card.Value);
                Assert.Equal("info", card.Status);
            },
            card =>
            {
                Assert.Equal("Email", card.Title);
                Assert.Equal("test@example.com", card.Value);
                Assert.Equal("ready", card.Status);
            });
    }

    [Fact]
    public async Task GetAsync_WithoutDisplayData_ReturnsEmptyCards()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-123",
            DisplayName = string.Empty,
            Email = string.Empty
        });

        IInitialDashboardReader reader = new InitialDashboardReader(currentUserService);

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.Equal(string.Empty, result.UserDisplayName);
        Assert.Empty(result.Cards);
    }
}
