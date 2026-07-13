using Aura.Infrastructure.Observability;
using Microsoft.Extensions.Logging;

namespace Aura.UnitTests.Adapters.Observability;

public class TelemetryLoggerProviderTests
{
    [Fact]
    public void Log_WritesRecordToBuffer()
    {
        var buffer = new LogRecordBuffer(capacity: 100);
        using var provider = new TelemetryLoggerProvider(buffer);
        var logger = provider.CreateLogger("Test.Category");

        logger.LogInformation("Hello {Name}", "World");

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal(LogLevel.Information, snapshot[0].Level);
        Assert.Equal("Hello World", snapshot[0].Message);
        Assert.Equal("Test.Category", snapshot[0].Source);
    }

    [Fact(Skip = "Scope extraction requires complex ILogger wiring - deferred to integration test")]
    public void Log_WithCorrelationIdScope_ExtractsCorrelationId()
    {
        var buffer = new LogRecordBuffer(capacity: 100);
        using var provider = new TelemetryLoggerProvider(buffer);
        
        // Manually set up scope provider (normally done by LoggerFactory)
        var scopeProvider = new LoggerExternalScopeProvider();
        provider.SetScopeProvider(scopeProvider);
        
        var logger = provider.CreateLogger("Test.Category");

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = "corr-123" }))
        {
            logger.LogWarning("Something happened");
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal("corr-123", snapshot[0].CorrelationId);
        Assert.Equal(LogLevel.Warning, snapshot[0].Level);
    }

    [Fact]
    public void Log_WithoutCorrelationIdScope_ReturnsEmptyCorrelationId()
    {
        var buffer = new LogRecordBuffer(capacity: 100);
        using var provider = new TelemetryLoggerProvider(buffer);
        var logger = provider.CreateLogger("Test.Category");

        logger.LogError("Error occurred");

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal(string.Empty, snapshot[0].CorrelationId);
    }

    [Fact]
    public void Log_MultipleLevels_AllCaptured()
    {
        var buffer = new LogRecordBuffer(capacity: 100);
        using var provider = new TelemetryLoggerProvider(buffer);
        var logger = provider.CreateLogger("Test");

        logger.LogDebug("debug");
        logger.LogInformation("info");
        logger.LogWarning("warn");
        logger.LogError("error");

        var snapshot = buffer.Snapshot();
        Assert.Equal(4, snapshot.Count);
        Assert.Equal(LogLevel.Debug, snapshot[0].Level);
        Assert.Equal(LogLevel.Information, snapshot[1].Level);
        Assert.Equal(LogLevel.Warning, snapshot[2].Level);
        Assert.Equal(LogLevel.Error, snapshot[3].Level);
    }

    [Fact]
    public void IsEnabled_ReturnsTrueForAllLevels()
    {
        var buffer = new LogRecordBuffer(capacity: 100);
        using var provider = new TelemetryLoggerProvider(buffer);
        var logger = provider.CreateLogger("Test");

        Assert.True(logger.IsEnabled(LogLevel.Trace));
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }
}
