using System.Collections.Concurrent;
using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.GraphConnector;

public class GraphConnectorDependencyInjectionTests
{
    [Fact]
    public void AddGraphConnectorAdapter_WhenEnabledAndRequiredFieldsMissing_EmitsWarningWithMissingFieldNames()
    {
        var services = new ServiceCollection();
        var logEntries = new ConcurrentQueue<string>();

        services.AddLogging(builder => builder.AddProvider(new InMemoryLoggerProvider(logEntries)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "true",
                ["GraphConnector:TenantId"] = "",
                ["GraphConnector:ClientId"] = "client-a"
            })
            .Build();

        services.AddGraphConnectorAdapter(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

        Assert.Contains(logEntries, entry =>
            entry.Contains("missing required Graph configuration fields", StringComparison.Ordinal)
            && entry.Contains("TenantId", StringComparison.Ordinal));
    }

    [Fact]
    public void AddGraphConnectorAdapter_WhenClientIdMissing_EmitsWarningWithClientId()
    {
        var services = new ServiceCollection();
        var logEntries = new ConcurrentQueue<string>();

        services.AddLogging(builder => builder.AddProvider(new InMemoryLoggerProvider(logEntries)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "true",
                ["GraphConnector:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["GraphConnector:ClientId"] = ""
            })
            .Build();

        services.AddGraphConnectorAdapter(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

        Assert.Contains(logEntries, entry =>
            entry.Contains("missing required Graph configuration fields", StringComparison.Ordinal)
            && entry.Contains("ClientId", StringComparison.Ordinal));
    }

    [Fact]
    public void AddGraphConnectorAdapter_WhenEnabledFalse_DoesNotEmitMissingFieldWarning()
    {
        var services = new ServiceCollection();
        var logEntries = new ConcurrentQueue<string>();

        services.AddLogging(builder => builder.AddProvider(new InMemoryLoggerProvider(logEntries)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "false",
                ["GraphConnector:TenantId"] = "",
                ["GraphConnector:ClientId"] = ""
            })
            .Build();

        services.AddGraphConnectorAdapter(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

        Assert.DoesNotContain(logEntries, entry =>
            entry.Contains("missing required Graph configuration fields", StringComparison.Ordinal));
    }

    [Fact]
    public void AddGraphConnectorAdapter_WhenEnabledWithValidGuidConfig_DoesNotEmitMissingFieldWarning()
    {
        var services = new ServiceCollection();
        var logEntries = new ConcurrentQueue<string>();

        services.AddLogging(builder => builder.AddProvider(new InMemoryLoggerProvider(logEntries)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphConnector:Enabled"] = "true",
                ["GraphConnector:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["GraphConnector:ClientId"] = "22222222-2222-2222-2222-222222222222"
            })
            .Build();

        services.AddGraphConnectorAdapter(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

        Assert.DoesNotContain(logEntries, entry =>
            entry.Contains("missing required Graph configuration fields", StringComparison.Ordinal));
    }

    private sealed class InMemoryLoggerProvider(ConcurrentQueue<string> entries) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(entries);

        public void Dispose()
        {
        }
    }

    private sealed class InMemoryLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
