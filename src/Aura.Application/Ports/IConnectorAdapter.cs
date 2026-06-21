using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IConnectorAdapter
{
    string ConnectorName { get; }

    Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct);
}
