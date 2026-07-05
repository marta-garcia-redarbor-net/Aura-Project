using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class DashboardPreviewApiClient : IDashboardPreviewApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public DashboardPreviewApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/dashboard/preview", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Propagate unauthorized so the UI can show a login overlay
            throw new UnauthorizedAccessException("Unauthorized when calling /api/dashboard/preview");
        }

        response.EnsureSuccessStatusCode();

        var preview = await response.Content.ReadFromJsonAsync<DashboardPreviewResponse>(SerializerOptions, cancellationToken);
        return preview ?? throw new InvalidOperationException("Aura.Api returned an empty dashboard preview payload.");
    }
}
