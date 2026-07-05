using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class DecisionLogApiClient : IDecisionLogApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public DecisionLogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DecisionLogResponse> GetDecisionsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"/api/triage/decisions?page={page}&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<DecisionLogResponse>(SerializerOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Aura.Api returned an empty triage decisions payload.");
    }
}
