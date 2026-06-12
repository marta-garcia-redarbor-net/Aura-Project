using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Unit tests for <see cref="DashboardApiClient"/>.
/// Verifies HTTP request behavior, deserialization, and error handling
/// using a stub <see cref="HttpMessageHandler"/> — no real network calls.
/// </summary>
public class DashboardApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetInitialDashboardAsync_WithPopulatedResponse_DeserializesCorrectly()
    {
        var expected = new InitialDashboardResponse(
            "Test User",
            [new DashboardCardResponse("Inbox", "7 pending", "info")]);

        var handler = new StubHandler(HttpStatusCode.OK, expected);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        var result = await client.GetInitialDashboardAsync(CancellationToken.None);

        Assert.Equal("Test User", result.UserDisplayName);
        var card = Assert.Single(result.Cards);
        Assert.Equal("Inbox", card.Title);
        Assert.Equal("7 pending", card.Value);
        Assert.Equal("info", card.Status);
    }

    [Fact]
    public async Task GetInitialDashboardAsync_WithEmptyCards_DeserializesEmptyList()
    {
        var expected = new InitialDashboardResponse("Test User", []);
        var handler = new StubHandler(HttpStatusCode.OK, expected);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        var result = await client.GetInitialDashboardAsync(CancellationToken.None);

        Assert.Equal("Test User", result.UserDisplayName);
        Assert.Empty(result.Cards);
    }

    [Fact]
    public async Task GetInitialDashboardAsync_WithNon200StatusCode_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetInitialDashboardAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetInitialDashboardAsync_With401_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetInitialDashboardAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetInitialDashboardAsync_WithNullJsonBody_ThrowsInvalidOperationException()
    {
        var handler = new StubHandler(HttpStatusCode.OK, content: "null");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetInitialDashboardAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetInitialDashboardAsync_RequestsCorrectPath()
    {
        var expected = new InitialDashboardResponse("Test User", []);
        var handler = new StubHandler(HttpStatusCode.OK, expected);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DashboardApiClient(httpClient);

        await client.GetInitialDashboardAsync(CancellationToken.None);

        Assert.Equal("/api/dashboard/initial", handler.LastRequestUri?.AbsolutePath);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpStatusCode statusCode, InitialDashboardResponse? response = null)
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
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
