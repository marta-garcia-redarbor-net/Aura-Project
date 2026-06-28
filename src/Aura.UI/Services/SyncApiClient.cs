using System.Net.Http.Json;
using System.Text.Json;

namespace Aura.UI.Services;

public sealed class SyncApiClient : ISyncApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public SyncApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/sync/status", cancellationToken);
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<List<SourceSyncStateDto>>(SerializerOptions, cancellationToken);
        return status ?? new List<SourceSyncStateDto>();
    }

    public async Task TriggerSyncAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/sync/now", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
