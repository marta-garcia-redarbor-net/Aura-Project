namespace Aura.Workers;

/// <summary>
/// Configuration options for the ConnectorExecutionWorker polling loop.
/// </summary>
public sealed class ConnectorExecutionOptions
{
    /// <summary>
    /// Interval in seconds between connector execution polling cycles.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;
}
