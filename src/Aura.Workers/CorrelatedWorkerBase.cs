using Microsoft.Extensions.Logging;

namespace Aura.Workers;

/// <summary>
/// Opt-in abstract base class for background workers that want per-cycle
/// correlation ID scoping. Generates a GUID per execution cycle and opens
/// an <c>ILogger.BeginScope</c> with <c>{{CorrelationId}}</c> before calling
/// <see cref="ExecuteCorrelatedAsync"/>.
///
/// Inherit from this class instead of <see cref="BackgroundService"/> to get
/// automatic correlation scoping without changing the existing cycle logic.
/// The derived class implements <see cref="ExecuteCorrelatedAsync"/> which
/// runs once per cycle and should include its own delay between cycles.
/// </summary>
public abstract class CorrelatedWorkerBase : BackgroundService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes the base worker with an <see cref="ILogger"/> for BeginScope.
    /// </summary>
    protected CorrelatedWorkerBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sealed execution loop. Generates a correlation ID, opens a BeginScope,
    /// and delegates to <see cref="ExecuteCorrelatedAsync"/> per cycle.
    /// </summary>
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid().ToString();
            using var _ = _logger.BeginScope("{CorrelationId}", correlationId);
            await ExecuteCorrelatedAsync(correlationId, stoppingToken);
        }
    }

    /// <summary>
    /// Override this to implement one cycle of the worker's work.
    /// The correlation ID is already scoped for all log calls within this method.
    /// Add any delay between cycles inside this method.
    /// </summary>
    protected abstract Task ExecuteCorrelatedAsync(
        string correlationId, CancellationToken stoppingToken);
}
