using System.Net;
using System.Text;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class DecisionLogApiClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetDecisionsAsync_WithPageQuery_ReturnsPagedResult()
    {
        var expected = new DecisionLogResponse(
            [
                new DecisionLogItemResponse(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Urgent PR review",
                    "pr-review",
                    "INTERRUPT",
                    88,
                    "High urgency",
                    DateTimeOffset.Parse("2026-07-05T12:00:00Z"),
                    "WindowOfOpportunity")
            ],
            50,
            2,
            20);

        var handler = new StubHandler(HttpStatusCode.OK, expected);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DecisionLogApiClient(httpClient);

        var result = await client.GetDecisionsAsync(page: 2, pageSize: 20, CancellationToken.None);

        Assert.Equal("/api/triage/decisions", handler.LastRequestUri?.AbsolutePath);
        Assert.Equal("?page=2&pageSize=20", handler.LastRequestUri?.Query);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDecisionsAsync_WithErrorStatus_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new DecisionLogApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetDecisionsAsync(1, 20, CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public Uri? LastRequestUri { get; private set; }

        public StubHandler(HttpStatusCode statusCode, DecisionLogResponse? response = null)
        {
            _statusCode = statusCode;
            _jsonContent = response is null ? null : JsonSerializer.Serialize(response, SerializerOptions);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(_statusCode);
            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
