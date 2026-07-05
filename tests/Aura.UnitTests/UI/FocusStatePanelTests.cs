using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.UI;

public sealed class FocusStatePanelTests : TestContext
{
    [Fact]
    public void FocusStatePanel_RendersDeepWorkWithBadge()
    {
        var client = Substitute.For<IFocusStateApiClient>();
        client.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse(
                CurrentState: "DeepWork",
                Label: null,
                Since: DateTimeOffset.UtcNow,
                Signals: ["resolved:DeepWork"])));

        Services.AddSingleton<IFocusStateRefreshScheduler>(Substitute.For<IFocusStateRefreshScheduler>());
        Services.AddSingleton(client);

        var cut = RenderComponent<FocusStatePanel>();

        cut.WaitForAssertion(() =>
        {
            var label = cut.Find("[data-testid='focus-state-label']");
            Assert.Equal("DeepWork", label.TextContent);
            var badge = cut.Find("[data-testid='focus-state-badge']");
            Assert.Contains("focus-state-badge--deepwork", badge.ClassList);
        });
    }

    [Fact]
    public void FocusStatePanel_UsesFiveMinuteRefreshInterval()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), FocusStatePanel.RefreshInterval);
    }

    [Fact]
    public void FocusStatePanel_WhenRefreshTimerFires_RefetchesFocusState()
    {
        var client = Substitute.For<IFocusStateApiClient>();
        client.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new FocusStateResponse("WindowOfOpportunity", null, DateTimeOffset.UtcNow, ["resolved:WindowOfOpportunity"])),
                Task.FromResult(new FocusStateResponse("DeepWork", null, DateTimeOffset.UtcNow, ["resolved:DeepWork"])));

        var scheduler = new TestRefreshScheduler();

        Services.AddSingleton(client);
        Services.AddSingleton<IFocusStateRefreshScheduler>(scheduler);

        var cut = RenderComponent<FocusStatePanel>();

        cut.WaitForAssertion(() =>
            Assert.Equal("WindowOfOpportunity", cut.Find("[data-testid='focus-state-label']").TextContent));

        Assert.Equal(TimeSpan.FromMinutes(5), scheduler.LastInterval);

        scheduler.FireAsync().GetAwaiter().GetResult();

        cut.WaitForAssertion(() =>
            Assert.Equal("DeepWork", cut.Find("[data-testid='focus-state-label']").TextContent));

        client.Received(2).GetCurrentAsync(Arg.Any<CancellationToken>());
    }

    private sealed class TestRefreshScheduler : IFocusStateRefreshScheduler
    {
        private Func<Task>? _callback;

        public TimeSpan LastInterval { get; private set; }

        public IDisposable StartRecurring(TimeSpan interval, Func<Task> callback)
        {
            LastInterval = interval;
            _callback = callback;
            return new NoopDisposable();
        }

        public Task FireAsync()
            => _callback?.Invoke() ?? Task.CompletedTask;

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
