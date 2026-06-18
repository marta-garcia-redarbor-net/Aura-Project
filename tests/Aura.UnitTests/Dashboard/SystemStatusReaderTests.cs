using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class SystemStatusReaderTests
{
    [Fact]
    public async Task GetStatusAsync_WhenAllHealthy_ReturnsOkForAllIndicators()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        mockAuth.IsConfigured().Returns(true);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Ok, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenQdrantUnhealthy_ReturnsErrorForQdrantOnly()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Unavailable);
        mockAuth.IsConfigured().Returns(true);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Error, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenMockAuthNotConfigured_ReturnsWarningForMockAuth()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        mockAuth.IsConfigured().Returns(false);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenQdrantDegraded_ReturnsWarningForQdrantOnly()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        mockAuth.IsConfigured().Returns(true);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Warning, result.Qdrant.State);
        Assert.Equal("Qdrant is reachable but reporting degraded health.", result.Qdrant.Microcopy);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenApiDegraded_ReturnsWarningForApiOnly()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        mockAuth.IsConfigured().Returns(true);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.Api.State);
        Assert.Equal("API endpoint is responding with degraded health.", result.Api.Microcopy);
        Assert.Equal(SystemIndicatorState.Ok, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_DerivesMockAuthFromProviderConfigurationOnly()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        mockAuth.IsConfigured().Returns(false);

        var reader = new SystemStatusReader(api, qdrant, mockAuth);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.MockAuth.State);
        await api.Received(1).GetReadinessAsync(Arg.Any<CancellationToken>());
        await qdrant.Received(1).GetReadinessAsync(Arg.Any<CancellationToken>());
        mockAuth.Received(1).IsConfigured();
    }
}
