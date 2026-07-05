using System.Net;
using System.Text;
using System.Text.Json;
using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class WorkItemsApiClientPriorityTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetBySourceAsync_DeserializesPriorityScore()
    {
        var payload = new[]
        {
            new WorkItemDetailResponse(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "ext-1",
                "Urgent item",
                "messages",
                "TeamsMessage",
                "Pending",
                "High",
                "1m ago",
                DateTimeOffset.Parse("2026-07-05T12:00:00Z"))
            {
                PriorityScore = 90
            }
        };

        var handler = new StubHandler(HttpStatusCode.OK, payload);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.aura.test") };
        var client = new WorkItemsApiClient(httpClient);

        var result = await client.GetBySourceAsync("TeamsMessage", "Pending", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(90, item.PriorityScore);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public StubHandler(HttpStatusCode statusCode, WorkItemDetailResponse[]? response = null)
        {
            _statusCode = statusCode;
            _jsonContent = response is null ? null : JsonSerializer.Serialize(response, SerializerOptions);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
