using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class SystemStatusApiClient : ISystemStatusApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public SystemStatusApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SystemStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/dashboard/system-status", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SystemStatusResponse>(SerializerOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Aura.Api returned an empty system status payload.");
    }

    public async Task<List<ErrorEntryDto>> GetRecentErrorsAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/dashboard/recent-errors", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<ErrorEntryDto>>(SerializerOptions, cancellationToken);
        return payload ?? [];
    }
}
