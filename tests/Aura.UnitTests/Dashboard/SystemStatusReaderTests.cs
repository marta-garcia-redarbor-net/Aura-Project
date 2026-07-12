using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class SystemStatusReaderTests
{
    private static (IApiReadinessProvider, IQdrantReadinessProvider, IMockAuthReadinessProvider,
        IDbReadinessProvider, ILlmReadinessProvider) CreateMocks()
    {
        var api = Substitute.For<IApiReadinessProvider>();
        var qdrant = Substitute.For<IQdrantReadinessProvider>();
        var mockAuth = Substitute.For<IMockAuthReadinessProvider>();
        var db = Substitute.For<IDbReadinessProvider>();
        var llm = Substitute.For<ILlmReadinessProvider>();

        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        mockAuth.IsConfigured().Returns(true);
        db.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);
        llm.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Healthy);

        return (api, qdrant, mockAuth, db, llm);
    }

    [Fact]
    public async Task GetStatusAsync_WhenAllHealthy_ReturnsOkForAllIndicators()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Ok, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
        Assert.Equal(SystemIndicatorState.Ok, result.Database.State);
        Assert.Equal(SystemIndicatorState.Ok, result.Llm.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenQdrantUnhealthy_ReturnsErrorForQdrantOnly()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Unavailable);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Error, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenMockAuthNotConfigured_ReturnsWarningForMockAuth()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        mockAuth.IsConfigured().Returns(false);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenQdrantDegraded_ReturnsWarningForQdrantOnly()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        qdrant.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Ok, result.Api.State);
        Assert.Equal(SystemIndicatorState.Warning, result.Qdrant.State);
        Assert.Equal("Qdrant is reachable but reporting degraded health.", result.Qdrant.Microcopy);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenApiDegraded_ReturnsWarningForApiOnly()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        api.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.Api.State);
        Assert.Equal("API endpoint is responding with degraded health.", result.Api.Microcopy);
        Assert.Equal(SystemIndicatorState.Ok, result.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Ok, result.MockAuth.State);
    }

    [Fact]
    public async Task GetStatusAsync_DerivesMockAuthFromProviderConfigurationOnly()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        mockAuth.IsConfigured().Returns(false);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.MockAuth.State);
        await api.Received(1).GetReadinessAsync(Arg.Any<CancellationToken>());
        await qdrant.Received(1).GetReadinessAsync(Arg.Any<CancellationToken>());
        mockAuth.Received(1).IsConfigured();
    }

    [Fact]
    public async Task GetStatusAsync_WhenDatabaseUnhealthy_ReturnsErrorForDatabase()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        db.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Unavailable);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Error, result.Database.State);
        Assert.Equal("Database is unreachable.", result.Database.Microcopy);
        Assert.Equal(SystemIndicatorState.Ok, result.Llm.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenDatabaseDegraded_ReturnsWarningForDatabase()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        db.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.Database.State);
        Assert.Equal("Database is reachable but reporting degraded health.", result.Database.Microcopy);
    }

    [Fact]
    public async Task GetStatusAsync_WhenLlmUnhealthy_ReturnsErrorForLlm()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        llm.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Unavailable);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Error, result.Llm.State);
        Assert.Equal("LLM (Ollama) is unreachable.", result.Llm.Microcopy);
    }

    [Fact]
    public async Task GetStatusAsync_WhenLlmDegraded_ReturnsWarningForLlm()
    {
        var (api, qdrant, mockAuth, db, llm) = CreateMocks();
        llm.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(ReadinessSignal.Degraded);
        var reader = new SystemStatusReader(api, qdrant, mockAuth, db, llm);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(SystemIndicatorState.Warning, result.Llm.State);
        Assert.Equal("LLM (Ollama) is reachable but reporting degraded health.", result.Llm.Microcopy);
    }
}
