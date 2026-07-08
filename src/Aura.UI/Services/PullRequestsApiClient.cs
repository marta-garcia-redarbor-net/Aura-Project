using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class PullRequestsApiClient : IPullRequestsApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public PullRequestsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("/api/pull-requests", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<PullRequestResponse[]>(SerializerOptions, ct);
        return items ?? [];
    }
}
