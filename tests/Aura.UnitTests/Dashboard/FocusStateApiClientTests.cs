using System.Net;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public sealed class FocusStateApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetCurrentAsync_RequestsExpectedPath()
    {
        var payload = new FocusStateResponse("DeepWork", null, DateTimeOffset.UtcNow, ["resolved:DeepWork"]);
        var handler = new StubHandler(HttpStatusCode.OK, payload);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new FocusStateApiClient(httpClient);

        await client.GetCurrentAsync(CancellationToken.None);

        Assert.Equal("/api/focus-state/current", handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetCurrentAsync_WithNullPayload_ThrowsInvalidOperationException()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "null");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new FocusStateApiClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCurrentAsync(CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpStatusCode statusCode, FocusStateResponse response)
        {
            _statusCode = statusCode;
            _jsonContent = JsonSerializer.Serialize(response, SerializerOptions);
        }

        public StubHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _jsonContent = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(_statusCode);
            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
