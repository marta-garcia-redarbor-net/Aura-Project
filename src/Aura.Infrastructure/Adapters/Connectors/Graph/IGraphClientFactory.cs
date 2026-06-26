using Microsoft.Graph;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Abstraction for creating authenticated <see cref="GraphServiceClient"/> instances.
/// Enables unit testing of Graph providers without real MSAL token acquisition.
/// </summary>
internal interface IGraphClientFactory
{
    /// <exception cref="MsalUiRequiredException">When no valid cached token for the given oid.</exception>
    Task<GraphServiceClient> CreateClientAsync(string oid, CancellationToken ct);
}
