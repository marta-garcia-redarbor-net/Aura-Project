using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Application.Services;

public sealed partial class GraphConnectorStatusReader : IGraphConnectorStatusReader
{
    private readonly IGraphConnectorSettingsProvider _settingsProvider;
    private readonly ILogger<GraphConnectorStatusReader> _logger;

    public GraphConnectorStatusReader(
        IGraphConnectorSettingsProvider settingsProvider,
        ILogger<GraphConnectorStatusReader> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public Task<GraphConnectorStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _settingsProvider.GetSettings();
        var state = DeriveState(settings);

        Log.StatusEvaluated(_logger, state, settings.Enabled, IsPresent(settings.TenantId), IsPresent(settings.ClientId), settings.HasValidCredentialsBlock);

        return Task.FromResult(new GraphConnectorStatusDto(state));
    }

    internal static GraphConnectorState DeriveState(GraphConnectorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
        {
            return GraphConnectorState.Disabled;
        }

        var hasTenant = IsPresent(settings.TenantId);
        var hasClient = IsPresent(settings.ClientId);

        if (!hasTenant && !hasClient)
        {
            return GraphConnectorState.MissingConfig;
        }

        if (!hasTenant || !hasClient || !settings.HasValidCredentialsBlock)
        {
            return GraphConnectorState.PartialConfig;
        }

        return GraphConnectorState.ValidConfig;
    }

    private static bool IsPresent(string? value) => !string.IsNullOrWhiteSpace(value);

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2101,
            Level = LogLevel.Information,
            Message = "Graph connector status evaluated as {State} (Enabled={Enabled}, HasTenantId={HasTenantId}, HasClientId={HasClientId}, HasValidCredentialsBlock={HasValidCredentialsBlock})")]
        public static partial void StatusEvaluated(
            ILogger logger,
            GraphConnectorState state,
            bool enabled,
            bool hasTenantId,
            bool hasClientId,
            bool hasValidCredentialsBlock);
    }
}
