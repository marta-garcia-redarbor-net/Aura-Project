using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class WorkItemsApiClient : IWorkItemsApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public WorkItemsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WorkItemDetailResponse>> GetBySourceAsync(
        string sourceType,
        string? status,
        CancellationToken cancellationToken)
    {
        var url = $"/api/workitems/?sourceType={Uri.EscapeDataString(sourceType)}";
        if (status is not null)
        {
            url += $"&status={Uri.EscapeDataString(status)}";
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Unauthorized when calling /api/workitems");
        }

        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<WorkItemDetailResponse[]>(SerializerOptions, cancellationToken);
        return items ?? [];
    }
}
