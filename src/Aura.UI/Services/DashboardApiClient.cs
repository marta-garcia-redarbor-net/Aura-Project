using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class DashboardApiClient : IDashboardApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/dashboard/initial", cancellationToken);
        response.EnsureSuccessStatusCode();

        var dashboard = await response.Content.ReadFromJsonAsync<InitialDashboardResponse>(SerializerOptions, cancellationToken);
        return dashboard ?? throw new InvalidOperationException("Aura.Api returned an empty dashboard payload.");
    }
}
