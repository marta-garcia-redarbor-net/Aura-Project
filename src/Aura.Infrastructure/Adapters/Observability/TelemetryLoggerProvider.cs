using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Observability;

/// <summary>
/// ILoggerProvider that captures log records into a ring buffer.
/// </summary>
public sealed class TelemetryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly LogRecordBuffer _buffer;
    private IExternalScopeProvider? _scopeProvider;

    public TelemetryLoggerProvider(LogRecordBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TelemetryLogger(categoryName, _buffer, _scopeProvider);
    }

    public void Dispose() { }

    private sealed class TelemetryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogRecordBuffer _buffer;
        private readonly IExternalScopeProvider? _scopeProvider;

        public TelemetryLogger(string categoryName, LogRecordBuffer buffer, IExternalScopeProvider? scopeProvider)
        {
            _categoryName = categoryName;
            _buffer = buffer;
            _scopeProvider = scopeProvider;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var correlationId = ExtractCorrelationId();
            var record = new LogRecordDto(
                logLevel,
                DateTimeOffset.UtcNow,
                correlationId,
                message,
                _categoryName);

            _buffer.Write(record);
        }

        private string ExtractCorrelationId()
        {
            if (_scopeProvider is null) return string.Empty;

            var correlationId = string.Empty;
            _scopeProvider.ForEachScope<object?>((scope, _) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object?>> scopeList)
                {
                    var kvp = scopeList.FirstOrDefault(k => k.Key == "CorrelationId");
                    if (kvp.Value is string id)
                    {
                        correlationId = id;
                    }
                }
            }, null);

            return correlationId;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
