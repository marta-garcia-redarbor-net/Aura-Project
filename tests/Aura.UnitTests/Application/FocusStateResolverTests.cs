using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Domain.FocusState;
using NSubstitute;

namespace Aura.UnitTests.Application;

public class FocusStateResolverTests
{
    private readonly IFocusStateOverrideStore _overrideStore;
    private readonly FocusStateResolver _resolver;

    public FocusStateResolverTests()
    {
        _overrideStore = Substitute.For<IFocusStateOverrideStore>();
        _resolver = new FocusStateResolver(_overrideStore);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoOverride_ReturnsWindowOfOpportunity()
    {
        _overrideStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FocusStateType?)null);

        var result = await _resolver.ResolveAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_WhenOverrideExists_ReturnsOverrideState()
    {
        _overrideStore.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(FocusStateType.DeepWork);

        var result = await _resolver.ResolveAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.DeepWork, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_WhenOverrideIsAway_ReturnsAway()
    {
        _overrideStore.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(FocusStateType.Away);

        var result = await _resolver.ResolveAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_ChecksOverrideBeforeAutoCompute()
    {
        _overrideStore.GetAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(FocusStateType.Recovery);

        var result = await _resolver.ResolveAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.Recovery, result.CurrentState);
        // Verify GetAsync was called (not the auto-compute path)
        await _overrideStore.Received(1).GetAsync("user-1", Arg.Any<CancellationToken>());
    }
}
