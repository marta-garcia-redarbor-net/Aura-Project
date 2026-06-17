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
            TenantId: "tenant-1",
            ClientId: "client-1",
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.Disabled, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithoutTenantAndClient_ReturnsMissingConfig()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: null,
            ClientId: null,
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.MissingConfig, result.State);
    }

    [Theory]
    [InlineData("tenant", null, true)]
    [InlineData(null, "client", true)]
    [InlineData("tenant", "client", false)]
    public async Task GetStatusAsync_WhenEnabledWithPartialRequiredFields_ReturnsPartialConfig(
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

        Assert.Equal(GraphConnectorState.PartialConfig, result.State);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEnabledWithAllRequiredFields_ReturnsValidConfig()
    {
        var settingsProvider = Substitute.For<IGraphConnectorSettingsProvider>();
        settingsProvider.GetSettings().Returns(new GraphConnectorSettings(
            Enabled: true,
            TenantId: "tenant-1",
            ClientId: "client-1",
            HasValidCredentialsBlock: true));

        var reader = new GraphConnectorStatusReader(settingsProvider, NullLogger<GraphConnectorStatusReader>.Instance);

        var result = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.ValidConfig, result.State);
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
