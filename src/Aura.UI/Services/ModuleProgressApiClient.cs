using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed class ModuleProgressApiClient : IModuleProgressApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ModuleProgressApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ModuleProgressResponse> GetAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/api/dashboard/module-progress", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ModuleProgressResponse>(SerializerOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Aura.Api returned an empty module progress payload.");
    }
}
