namespace Aura.Workers;

public sealed partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                Log.WorkerRunning(_logger, DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3301, Level = LogLevel.Information,
            Message = "Worker running at: {time}")]
        public static partial void WorkerRunning(ILogger logger, DateTimeOffset time);
    }
}
