using System.Net;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class SystemStatusApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetStatusAsync_RequestsExpectedPath()
    {
        var payload = new SystemStatusResponse(
            new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
            new SystemIndicatorResponse(SystemIndicatorStateResponse.Warning, "warn"),
            new SystemIndicatorResponse(SystemIndicatorStateResponse.Error, "err"));

        var handler = new StubHandler(HttpStatusCode.OK, payload);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new SystemStatusApiClient(httpClient);

        await client.GetStatusAsync(CancellationToken.None);

        Assert.Equal("/api/dashboard/system-status", handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetStatusAsync_WithNonSuccessStatus_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.BadGateway);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new SystemStatusApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStatusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusAsync_WithNullPayload_ThrowsInvalidOperationException()
    {
        var handler = new StubHandler(HttpStatusCode.OK, content: "null");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new SystemStatusApiClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetStatusAsync(CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpStatusCode statusCode, SystemStatusResponse? response = null)
        {
            _statusCode = statusCode;
            _jsonContent = response is not null
                ? JsonSerializer.Serialize(response, SerializerOptions)
                : null;
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
