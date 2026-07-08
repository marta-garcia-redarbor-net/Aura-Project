using System.Reflection;
using Aura.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Aura.UnitTests.TestDoubles.Observability;

namespace Aura.UnitTests.Workers;

public class CorrelatedWorkerBaseTests
{
    [Fact]
    public void IsAbstractBackgroundService()
    {
        Assert.True(typeof(CorrelatedWorkerBase).IsAbstract);
        Assert.True(typeof(CorrelatedWorkerBase).IsSubclassOf(typeof(BackgroundService)));
    }

    [Fact]
    public void HasExecuteCorrelatedAsyncAbstractMethod()
    {
        var method = typeof(CorrelatedWorkerBase).GetMethod("ExecuteCorrelatedAsync",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method!.IsAbstract);
        Assert.True(method.ReturnType == typeof(Task));

        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.Name == "correlationId" && p.ParameterType == typeof(string));
        Assert.Contains(parameters, p => p.Name == "stoppingToken" && p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public async Task ExecuteCorrelatedAsync_ReceivesNonEmptyCorrelationId()
    {
        string? capturedCorrelationId = null;
        var worker = new TestCorrelatedWorker(NullLogger<CorrelatedWorkerBase>.Instance, (corrId, _) =>
        {
            capturedCorrelationId = corrId;
            return Task.CompletedTask;
        });

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(capturedCorrelationId);
        Assert.NotEmpty(capturedCorrelationId);
        Assert.True(Guid.TryParse(capturedCorrelationId, out _), "Correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task ExecuteCorrelatedAsync_ReceivesDifferentIdEachCycle()
    {
        var ids = new List<string>();
        var worker = new TestCorrelatedWorker(NullLogger<CorrelatedWorkerBase>.Instance, (corrId, _) =>
        {
            ids.Add(corrId);
            return Task.CompletedTask;
        });

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.True(ids.Count >= 2, $"Expected at least 2 cycles but got {ids.Count}");
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Fact]
    public async Task ExecuteCorrelatedAsync_WorkerLogsCarryCycleCorrelationId()
    {
        var logger = new ScopeAwareTestLogger<CorrelatedWorkerBase>();
        var correlationIds = new List<string>();

        var worker = new TestCorrelatedWorker(logger, (corrId, _) =>
        {
            correlationIds.Add(corrId);
            logger.LogInformation("Worker cycle executed");
            return Task.CompletedTask;
        });

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.NotEmpty(correlationIds);
        var firstCycleId = correlationIds[0];
        var firstCycleLog = logger.Entries.First(e => e.Message.Contains("Worker cycle executed", StringComparison.Ordinal));
        Assert.Equal(firstCycleId, firstCycleLog.Scope["CorrelationId"]?.ToString());
    }

    /// <summary>
    /// Concrete test implementation of the abstract CorrelatedWorkerBase.
    /// </summary>
    private sealed class TestCorrelatedWorker : CorrelatedWorkerBase
    {
        private readonly Func<string, CancellationToken, Task> _onExecute;

        public TestCorrelatedWorker(ILogger logger, Func<string, CancellationToken, Task> onExecute)
            : base(logger)
        {
            _onExecute = onExecute;
        }

        protected override async Task ExecuteCorrelatedAsync(string correlationId, CancellationToken stoppingToken)
        {
            await _onExecute(correlationId, stoppingToken);
        }
    }
}
