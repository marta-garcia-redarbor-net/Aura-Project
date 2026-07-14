using Aura.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure;

/// <summary>
/// DI registration extension for observability telemetry buffers and listeners.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers telemetry buffers and listeners for the observability dashboard.
    /// </summary>
    public static IServiceCollection AddAuraObservability(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Buffers (singletons — shared across all consumers)
        services.AddSingleton<LogRecordBuffer>(new LogRecordBuffer(capacity: 1000));
        services.AddSingleton<SpanBuffer>(new SpanBuffer(capacity: 500));
        services.AddSingleton<MetricSnapshotBuffer>(new MetricSnapshotBuffer(capacity: 100));

        // Listeners (singletons — subscribe to .NET diagnostics)
        services.AddSingleton<TelemetryActivityListener>(sp =>
            new TelemetryActivityListener(sp.GetRequiredService<SpanBuffer>()));

        services.AddSingleton<TelemetryMeterListener>(sp =>
            new TelemetryMeterListener(sp.GetRequiredService<MetricSnapshotBuffer>()));

        // Logger provider (subscribes to ILoggerFactory)
        services.AddSingleton<ILoggerProvider, TelemetryLoggerProvider>(sp =>
            new TelemetryLoggerProvider(sp.GetRequiredService<LogRecordBuffer>()));

        return services;
    }
}
