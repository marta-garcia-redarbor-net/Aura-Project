using Microsoft.Graph;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Abstraction for creating authenticated <see cref="GraphServiceClient"/> instances.
/// Enables unit testing of Graph providers without real MSAL token acquisition.
/// </summary>
internal interface IGraphClientFactory
{
    Task<GraphServiceClient> CreateClientAsync(CancellationToken ct);
}
