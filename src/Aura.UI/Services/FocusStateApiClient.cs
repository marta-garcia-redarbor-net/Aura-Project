using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class FocusStateApiClient : IFocusStateApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public FocusStateApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FocusStateResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/focus-state/current", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FocusStateResponse>(SerializerOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Aura.Api returned an empty focus state payload.");
    }
}
