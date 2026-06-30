using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class GraphConnectorApiClient : IGraphConnectorApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    private readonly HttpClient _httpClient;

    public GraphConnectorApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GraphConnectorStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/connectors/graph/status", cancellationToken);
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<GraphConnectorStatusResponse>(SerializerOptions, cancellationToken);
        return status ?? throw new InvalidOperationException("Aura.Api returned an empty graph connector status payload.");
    }
}
