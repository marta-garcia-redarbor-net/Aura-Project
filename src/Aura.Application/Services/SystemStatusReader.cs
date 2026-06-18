using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

public sealed class SystemStatusReader : ISystemStatusReader
{
    private readonly IApiReadinessProvider _apiReadinessProvider;
    private readonly IQdrantReadinessProvider _qdrantReadinessProvider;
    private readonly IMockAuthReadinessProvider _mockAuthReadinessProvider;

    public SystemStatusReader(
        IApiReadinessProvider apiReadinessProvider,
        IQdrantReadinessProvider qdrantReadinessProvider,
        IMockAuthReadinessProvider mockAuthReadinessProvider)
    {
        ArgumentNullException.ThrowIfNull(apiReadinessProvider);
        ArgumentNullException.ThrowIfNull(qdrantReadinessProvider);
        ArgumentNullException.ThrowIfNull(mockAuthReadinessProvider);

        _apiReadinessProvider = apiReadinessProvider;
        _qdrantReadinessProvider = qdrantReadinessProvider;
        _mockAuthReadinessProvider = mockAuthReadinessProvider;
    }

    public async Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var apiReadiness = await _apiReadinessProvider.GetReadinessAsync(cancellationToken);
        var qdrantReadiness = await _qdrantReadinessProvider.GetReadinessAsync(cancellationToken);
        var mockAuthConfigured = _mockAuthReadinessProvider.IsConfigured();

        return new SystemStatusDto(
            Api: DeriveApiIndicator(apiReadiness),
            Qdrant: DeriveQdrantIndicator(qdrantReadiness),
            MockAuth: DeriveMockAuthIndicator(mockAuthConfigured));
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
}
