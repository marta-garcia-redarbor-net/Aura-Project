using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Generic port for fetching messages from an external source (e.g., Teams, Outlook).
/// Implementations live in Infrastructure; the generic type parameter is an infrastructure-internal DTO.
/// </summary>
public interface IMessageSourceProvider<T>
{
    Task<IReadOnlyList<T>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct);
}
