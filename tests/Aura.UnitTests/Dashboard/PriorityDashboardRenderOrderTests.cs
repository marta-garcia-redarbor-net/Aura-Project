using System.Security.Claims;
using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="PriorityDashboard"/> renders <see cref="RankedSummaryList"/>
/// before the connector-cards grid, as specified by the graph-ui-polish change.
/// </summary>
public class PriorityDashboardRenderOrderTests : TestContext
{
    /// <summary>
    /// Minimal IAuthorizationService that always returns Authorized.
    /// Required because AuthorizeView inside PriorityDashboard calls IAuthorizationService.
    /// </summary>
    private sealed class AlwaysAuthorizedService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private void SetupCommonServices()
    {
        // --- Mocks for PriorityDashboard itself ---
        var syncApi = Substitute.For<ISyncApiClient>();
        syncApi.GetSyncStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<SourceSyncStateDto>
            {
                new() { Source = "Outlook", ItemCount = 5 }
            }));

        var graphApi = Substitute.For<IGraphConnectorApiClient>();
        graphApi.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphConnectorStatusResponse("ValidConfig")));

        Services.AddSingleton<ISyncApiClient>(syncApi);
        Services.AddSingleton<IGraphConnectorApiClient>(graphApi);

        // --- Authorization ---
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        // --- Child component: InboxPreviewPanel & MorningSummaryPreviewPanel ---
        var previewApi = Substitute.For<IDashboardPreviewApiClient>();
        previewApi.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse(
                [],
                [])));
        Services.AddSingleton<IDashboardPreviewApiClient>(previewApi);

        // --- Child component: UpcomingMeetingsPanel (needs GetUpcomingMeetingsUseCase) ---
        var calendarStore = Substitute.For<ICalendarEventStore>();
        calendarStore.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>([]));
        Services.AddSingleton<GetUpcomingMeetingsUseCase>(
            new GetUpcomingMeetingsUseCase(calendarStore));
    }

    private static AuthenticationState CreateAuthorizedState()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "Test User") },
            "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    [Fact]
    public void PriorityDashboard_RankedSummaryListRendersBeforeConnectorCards()
    {
        // Arrange
        SetupCommonServices();

        var state = new DashboardViewState(
            DashboardViewStateKind.Populated,
            "Test User",
            [],
            "Ready");

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<PriorityDashboard>(parameters => parameters
            .AddCascadingValue(authStateTask)
            .AddCascadingValue(state));

        // Assert: RankedSummaryList appears before connector-cards in the markup
        var markup = cut.Markup;
        var rankedIndex = markup.IndexOf("data-testid=\"ranked-summary-list\"", StringComparison.Ordinal);
        var connectorIndex = markup.IndexOf("data-testid=\"connector-cards\"", StringComparison.Ordinal);

        Assert.True(rankedIndex >= 0, "RankedSummaryList should be rendered");
        Assert.True(connectorIndex >= 0, "connector-cards should be rendered");
        Assert.True(rankedIndex < connectorIndex,
            $"RankedSummaryList (index {rankedIndex}) should appear before connector-cards (index {connectorIndex})");
    }

    [Fact]
    public void PriorityDashboard_RankedSummaryListStillRendersAfterLoading()
    {
        // Arrange
        SetupCommonServices();

        var state = new DashboardViewState(
            DashboardViewStateKind.Populated,
            "Test User",
            [],
            "Ready");

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<PriorityDashboard>(parameters => parameters
            .AddCascadingValue(authStateTask)
            .AddCascadingValue(state));

        // Assert: both components are rendered
        Assert.Contains("ranked-summary-list", cut.Markup);
        Assert.Contains("connector-cards", cut.Markup);
    }

    [Fact]
    public void PriorityDashboard_DoesNotRenderHealthPanels()
    {
        // Arrange
        SetupCommonServices();

        var state = new DashboardViewState(
            DashboardViewStateKind.Populated,
            "Test User",
            [],
            "Ready");

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<PriorityDashboard>(parameters => parameters
            .AddCascadingValue(authStateTask)
            .AddCascadingValue(state));

        // Assert: health panels must NOT appear (they live on /health now)
        Assert.DoesNotContain("graph-connector-panel", cut.Markup);
        Assert.DoesNotContain("system-status-panel", cut.Markup);
        Assert.DoesNotContain("module-progress-panel", cut.Markup);
    }
}
