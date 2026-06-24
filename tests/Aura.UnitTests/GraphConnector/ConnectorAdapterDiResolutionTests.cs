using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.GraphConnector;

/// <summary>
/// Proves that when IMessageSourceProvider&lt;T&gt; is registered in DI,
/// the connector adapters receive it (non-null) and use it for fetching.
/// This proves the adapter injection path for tasks 3.8, 3.9, and 3.10.
/// </summary>
public class ConnectorAdapterDiResolutionTests
{
    [Fact]
    public void TeamsAdapter_ResolvesWithSourceProvider_WhenRegistered()
    {
        var services = new ServiceCollection();
        var mockProvider = Substitute.For<IMessageSourceProvider<TeamsMessageDto>>();

        services.AddSingleton(mockProvider);
        services.AddSingleton<IWorkItemBuffer, TestBuffer>();
        services.AddSingleton<TeamsWorkItemMapper>();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddScoped<TeamsConnectorAdapter>(sp => new TeamsConnectorAdapter(
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TeamsConnectorAdapter>>(),
            sp.GetRequiredService<IWorkItemBuffer>(),
            sp.GetRequiredService<TeamsWorkItemMapper>(),
            sourceProvider: sp.GetService<IMessageSourceProvider<TeamsMessageDto>>()));

        using var provider = services.BuildServiceProvider();
        var adapter = provider.GetRequiredService<TeamsConnectorAdapter>();

        // Prove the adapter was constructed with a non-null source provider
        // by executing and verifying the mock provider is called
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("teams", "messages", "acme"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

        mockProvider.FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamsMessageDto>>([]));

        var result = adapter.ExecuteAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        mockProvider.Received(1).FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void OutlookAdapter_ResolvesWithSourceProvider_WhenRegistered()
    {
        var services = new ServiceCollection();
        var mockProvider = Substitute.For<IMessageSourceProvider<OutlookEmailDto>>();

        services.AddSingleton(mockProvider);
        services.AddSingleton<IWorkItemBuffer, TestBuffer>();
        services.AddSingleton<OutlookWorkItemMapper>();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddScoped<OutlookConnectorAdapter>(sp => new OutlookConnectorAdapter(
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutlookConnectorAdapter>>(),
            sp.GetRequiredService<IWorkItemBuffer>(),
            sp.GetRequiredService<OutlookWorkItemMapper>(),
            sourceProvider: sp.GetService<IMessageSourceProvider<OutlookEmailDto>>()));

        using var provider = services.BuildServiceProvider();
        var adapter = provider.GetRequiredService<OutlookConnectorAdapter>();

        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("outlook", "inbox", "acme"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

        mockProvider.FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutlookEmailDto>>([]));

        var result = adapter.ExecuteAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        mockProvider.Received(1).FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void TeamsAdapter_ResolvesWithNullProvider_WhenNotRegistered_UsesFixtures()
    {
        var services = new ServiceCollection();

        // Do NOT register IMessageSourceProvider<TeamsMessageDto>
        services.AddSingleton<IWorkItemBuffer, TestBuffer>();
        services.AddSingleton<TeamsWorkItemMapper>();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddScoped<TeamsConnectorAdapter>(sp => new TeamsConnectorAdapter(
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TeamsConnectorAdapter>>(),
            sp.GetRequiredService<IWorkItemBuffer>(),
            sp.GetRequiredService<TeamsWorkItemMapper>(),
            sourceProvider: sp.GetService<IMessageSourceProvider<TeamsMessageDto>>()));

        using var provider = services.BuildServiceProvider();
        var adapter = provider.GetRequiredService<TeamsConnectorAdapter>();

        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("teams", "messages", "acme"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

        var result = adapter.ExecuteAsync(request, CancellationToken.None).GetAwaiter().GetResult();

        // Fixture path: default fixtures have 3 items (2 valid, 1 invalid)
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
    }

    private sealed class TestBuffer : IWorkItemBuffer
    {
        public void Enqueue(Aura.Domain.WorkItems.WorkItem item) { }
        public IReadOnlyList<Aura.Domain.WorkItems.WorkItem> Drain() => [];
    }
}
