using System.Net;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class ModuleProgressApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetAsync_RequestsExpectedPath()
    {
        var payload = new ModuleProgressResponse(
            [new ModuleEntryResponse("module-1", ModuleProgressStateResponse.Pending)],
            IsSeeded: true);

        var handler = new StubHandler(HttpStatusCode.OK, payload);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new ModuleProgressApiClient(httpClient);

        await client.GetAsync(CancellationToken.None);

        Assert.Equal("/api/dashboard/module-progress", handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetAsync_WithNonSuccessStatus_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new ModuleProgressApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WithNullPayload_ThrowsInvalidOperationException()
    {
        var handler = new StubHandler(HttpStatusCode.OK, content: "null");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new ModuleProgressApiClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpStatusCode statusCode, ModuleProgressResponse? response = null)
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
