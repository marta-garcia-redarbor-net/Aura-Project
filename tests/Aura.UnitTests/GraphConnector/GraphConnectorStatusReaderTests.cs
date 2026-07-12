using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.GraphConnector;

public class GraphConnectorStatusReaderTests
{
    [Fact]
    public async Task GetStatusAsync_WhenDisabled_ReturnsDisabledEvenWithFullConfig()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: false,
            TenantId: "11111111-1111-1111-1111-111111111111",
            ClientId: "22222222-2222-2222-2222-222222222222",
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithoutTenantAndClient_ReturnsDisabled()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: null,
            ClientId: null,
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }

    [Theory]
    [InlineData("tenant", null, true)]
    [InlineData(null, "client", true)]
    [InlineData("tenant", null, false)]
    [InlineData(null, "client", false)]
    public async Task GetStatusAsync_WhenEnabledWithPartialRequiredFields_ReturnsDisabled(
        string? tenantId,
        string? clientId,
        bool hasValidCredentialsBlock)
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: tenantId,
            ClientId: clientId,
            HasValidCredentialsBlock: hasValidCredentialsBlock));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithAllRequiredFields_ReturnsValidConfig()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: "11111111-1111-1111-1111-111111111111",
            ClientId: "22222222-2222-2222-2222-222222222222",
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.ValidConfig, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithAllFields_ButNoCredentialsBlock_ReturnsValidConfig()
    {
        // Delegated flow: ClientId + TenantId is sufficient, no credentials block needed
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: "11111111-1111-1111-1111-111111111111",
            ClientId: "22222222-2222-2222-2222-222222222222",
            HasValidCredentialsBlock: false));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.ValidConfig, result.State);
    }

    [Theory]
    [InlineData("invalid", "22222222-2222-2222-2222-222222222222")]
    [InlineData("11111111-1111-1111-1111-111111111111", "invalid")]
    public async Task GetStatusAsync_WhenEnabledWithInvalidGuidFields_ReturnsDisabled(
        string tenantId,
        string clientId)
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: tenantId,
            ClientId: clientId,
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenDisabledAndMissingFields_DisabledTakesPrecedence()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: false,
            TenantId: null,
            ClientId: null,
            HasValidCredentialsBlock: false));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }
}
