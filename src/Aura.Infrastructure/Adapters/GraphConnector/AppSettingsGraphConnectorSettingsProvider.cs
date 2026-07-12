using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.GraphConnector;

internal sealed class AppSettingsGraphConnectorSettingsProvider : IGraphConnectorSettingsProvider
{
    private readonly IOptionsMonitor<GraphConnectorOptions> _options;

    public AppSettingsGraphConnectorSettingsProvider(IOptionsMonitor<GraphConnectorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public GraphConnectorSettings GetSettings()
    {
        var options = _options.CurrentValue;

        return new GraphConnectorSettings(
            Enabled: options.Enabled,
            TenantId: options.TenantId,
            ClientId: options.ClientId,
            HasValidCredentialsBlock: HasValidCredentials(options));
    }

    private static bool HasValidCredentials(GraphConnectorOptions options)
    {
        // Delegated flow needs no client secret — ClientId + TenantId is sufficient
        return IsValidGuid(options.ClientId)
               && IsValidGuid(options.TenantId);
    }

    private static bool IsValidGuid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Guid.TryParse(value, out _);
}
