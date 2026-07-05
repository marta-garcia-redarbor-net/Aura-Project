using System.Net;
using System.Text;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class FocusStateApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetCurrentAsync_WithPayload_ReturnsFocusState()
    {
        var expected = new FocusStateResponse("DeepWork", true, "user-123");
        var handler = new StubHandler(HttpStatusCode.OK, expected);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new FocusStateApiClient(httpClient);

        var result = await client.GetCurrentAsync(CancellationToken.None);

        Assert.Equal("DeepWork", result.State);
        Assert.True(result.IsOverridden);
        Assert.Equal("user-123", result.UserId);
    }

    [Fact]
    public async Task SetOverrideAsync_SendsPutWithStatePayload()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new FocusStateApiClient(httpClient);

        await client.SetOverrideAsync("Away", CancellationToken.None);

        Assert.Equal(HttpMethod.Put, handler.LastMethod);
        Assert.Equal("/api/focus-state", handler.LastRequestUri?.AbsolutePath);
        Assert.Contains("\"state\":\"Away\"", handler.LastBody ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClearOverrideAsync_SendsDelete()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new FocusStateApiClient(httpClient);

        await client.ClearOverrideAsync(CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Equal("/api/focus-state", handler.LastRequestUri?.AbsolutePath);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public string? LastBody { get; private set; }

        public StubHandler(HttpStatusCode statusCode, FocusStateResponse? response = null)
        {
            _statusCode = statusCode;
            _jsonContent = response is null ? null : JsonSerializer.Serialize(response, SerializerOptions);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastMethod = request.Method;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(_statusCode);
            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}
