using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

public sealed class SystemStatusReader : ISystemStatusReader
{
    private readonly IApiReadinessProvider _apiReadinessProvider;
    private readonly IQdrantReadinessProvider _qdrantReadinessProvider;
    private readonly IMockAuthReadinessProvider _mockAuthReadinessProvider;
    private readonly IDbReadinessProvider _dbReadinessProvider;
    private readonly ILlmReadinessProvider _llmReadinessProvider;

    public SystemStatusReader(
        IApiReadinessProvider apiReadinessProvider,
        IQdrantReadinessProvider qdrantReadinessProvider,
        IMockAuthReadinessProvider mockAuthReadinessProvider,
        IDbReadinessProvider dbReadinessProvider,
        ILlmReadinessProvider llmReadinessProvider)
    {
        ArgumentNullException.ThrowIfNull(apiReadinessProvider);
        ArgumentNullException.ThrowIfNull(qdrantReadinessProvider);
        ArgumentNullException.ThrowIfNull(mockAuthReadinessProvider);
        ArgumentNullException.ThrowIfNull(dbReadinessProvider);
        ArgumentNullException.ThrowIfNull(llmReadinessProvider);

        _apiReadinessProvider = apiReadinessProvider;
        _qdrantReadinessProvider = qdrantReadinessProvider;
        _mockAuthReadinessProvider = mockAuthReadinessProvider;
        _dbReadinessProvider = dbReadinessProvider;
        _llmReadinessProvider = llmReadinessProvider;
    }

    public async Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Run all async providers in parallel so the total time = max(latency) not sum
        var apiTask = _apiReadinessProvider.GetReadinessAsync(cancellationToken);
        var dbTask = _dbReadinessProvider.GetReadinessAsync(cancellationToken);
        var qdrantTask = _qdrantReadinessProvider.GetReadinessAsync(cancellationToken);
        var llmTask = _llmReadinessProvider.GetReadinessAsync(cancellationToken);

        await Task.WhenAll(apiTask, dbTask, qdrantTask, llmTask);

        var mockAuthConfigured = _mockAuthReadinessProvider.IsConfigured();

        return new SystemStatusDto(
            Api: DeriveApiIndicator(apiTask.Result),
            Qdrant: DeriveQdrantIndicator(qdrantTask.Result),
            MockAuth: DeriveMockAuthIndicator(mockAuthConfigured),
            Database: DeriveDatabaseIndicator(dbTask.Result),
            Llm: DeriveLlmIndicator(llmTask.Result));
    }

    private static SystemIndicatorDto DeriveApiIndicator(ReadinessSignal readiness)
        => readiness switch
        {
            ReadinessSignal.Healthy => new SystemIndicatorDto(SystemIndicatorState.Ok, "API endpoint is reachable and responding."),
            ReadinessSignal.Degraded => new SystemIndicatorDto(SystemIndicatorState.Warning, "API endpoint is responding with degraded health."),
            _ => new SystemIndicatorDto(SystemIndicatorState.Error, "API endpoint is not responding.")
        };

    private static SystemIndicatorDto DeriveQdrantIndicator(ReadinessSignal readiness)
        => readiness switch
        {
            ReadinessSignal.Healthy => new SystemIndicatorDto(SystemIndicatorState.Ok, "Qdrant is reachable."),
            ReadinessSignal.Degraded => new SystemIndicatorDto(SystemIndicatorState.Warning, "Qdrant is reachable but reporting degraded health."),
            _ => new SystemIndicatorDto(SystemIndicatorState.Error, "Qdrant is unreachable.")
        };

    private static SystemIndicatorDto DeriveMockAuthIndicator(bool isConfigured)
        => isConfigured
            ? new SystemIndicatorDto(SystemIndicatorState.Ok, "Mock auth provider is configured.")
            : new SystemIndicatorDto(SystemIndicatorState.Warning, "Mock auth provider is not configured.");

    private static SystemIndicatorDto DeriveDatabaseIndicator(ReadinessSignal readiness)
        => readiness switch
        {
            ReadinessSignal.Healthy => new SystemIndicatorDto(SystemIndicatorState.Ok, "Database is reachable."),
            ReadinessSignal.Degraded => new SystemIndicatorDto(SystemIndicatorState.Warning, "Database is reachable but reporting degraded health."),
            _ => new SystemIndicatorDto(SystemIndicatorState.Error, "Database is unreachable.")
        };

    private static SystemIndicatorDto DeriveLlmIndicator(ReadinessSignal readiness)
        => readiness switch
        {
            ReadinessSignal.Healthy => new SystemIndicatorDto(SystemIndicatorState.Ok, "LLM (Ollama) is reachable."),
            ReadinessSignal.Degraded => new SystemIndicatorDto(SystemIndicatorState.Warning, "LLM (Ollama) is reachable but reporting degraded health."),
            _ => new SystemIndicatorDto(SystemIndicatorState.Error, "LLM (Ollama) is unreachable.")
        };
}
