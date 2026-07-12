using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

public class SystemStatusPanelErrorTests : TestContext
{
    [Fact]
    public void SystemStatusPanel_RendersErrors_WhenPresent()
    {
        var errors = new List<ErrorEntryDto>
        {
            new("corr-abc", new DateTimeOffset(2026, 7, 6, 10, 30, 0, TimeSpan.Zero), "Something went wrong"),
            new("corr-xyz", new DateTimeOffset(2026, 7, 6, 10, 31, 0, TimeSpan.Zero), "Another error"),
        };

        Services.AddScoped<ISystemStatusApiClient>(_ => new FakeSystemStatusApiClient(errors: errors));

        var cut = RenderComponent<SystemStatusPanel>();

        var errorItems = cut.FindAll("[data-testid='recent-error-item']");
        Assert.Equal(2, errorItems.Count);
        Assert.Contains("corr-abc", errorItems[0].TextContent);
        Assert.Contains("corr-xyz", errorItems[1].TextContent);
    }

    [Fact]
    public void SystemStatusPanel_RendersNoErrorsSection_WhenEmpty()
    {
        Services.AddScoped<ISystemStatusApiClient>(_ => new FakeSystemStatusApiClient(errors: []));

        var cut = RenderComponent<SystemStatusPanel>();

        var errorItems = cut.FindAll("[data-testid='recent-error-item']");
        Assert.Empty(errorItems);

        var noErrors = cut.FindAll("[data-testid='no-recent-errors']");
        Assert.Single(noErrors);
    }

    [Fact]
    public void SystemStatusPanel_PreservesReadinessIndicators_WhenErrorsPresent()
    {
        var errors = new List<ErrorEntryDto>
        {
            new("corr-1", DateTimeOffset.UtcNow, "error"),
        };

        Services.AddScoped<ISystemStatusApiClient>(_ => new FakeSystemStatusApiClient(errors: errors));

        var cut = RenderComponent<SystemStatusPanel>();

        // Readiness list should still be present
        var statusList = cut.FindAll("[data-testid='system-status-list']");
        Assert.Single(statusList);
    }

    private sealed class FakeSystemStatusApiClient : ISystemStatusApiClient
    {
        private readonly List<ErrorEntryDto> _errors;

        public FakeSystemStatusApiClient(List<ErrorEntryDto>? errors = null)
        {
            _errors = errors ?? [];
        }

        public Task<SystemStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok")));
        }

        public Task<List<ErrorEntryDto>> GetRecentErrorsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_errors);
        }
    }
}
