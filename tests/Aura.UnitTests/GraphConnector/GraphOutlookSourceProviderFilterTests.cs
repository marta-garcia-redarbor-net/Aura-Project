using System.Net;
using System.Text;
using System.Text.Json;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Aura.UnitTests.GraphConnector;

public class GraphOutlookSourceProviderFilterTests
{
    [Fact]
    public async Task FetchAsync_QueryContainsFilterIsReadEqFalse()
    {
        // Arrange
        var recordingHandler = new RecordingHttpMessageHandler("""{"value":[]}""");
        var provider = CreateProvider(recordingHandler);

        // Act
        await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.NotNull(recordingHandler.LastRequestUri);
        var query = Uri.UnescapeDataString(recordingHandler.LastRequestUri.Query);
        Assert.Contains("$filter=isRead eq false", query);
    }

    [Fact]
    public async Task FetchAsync_QueryContainsEndpointInboxMessages()
    {
        // Arrange
        var recordingHandler = new RecordingHttpMessageHandler("""{"value":[]}""");
        var provider = CreateProvider(recordingHandler);

        // Act
        await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.NotNull(recordingHandler.LastRequestUri);
        var path = Uri.UnescapeDataString(recordingHandler.LastRequestUri.AbsolutePath);
        Assert.Contains("/me/mailFolders/inbox/messages", path);
    }

    [Fact]
    public async Task FetchAsync_QueryContainsIsReadInSelect()
    {
        // Arrange
        var recordingHandler = new RecordingHttpMessageHandler("""{"value":[]}""");
        var provider = CreateProvider(recordingHandler);

        // Act
        await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.NotNull(recordingHandler.LastRequestUri);
        var query = Uri.UnescapeDataString(recordingHandler.LastRequestUri.Query);
        Assert.Contains("isRead", query);
    }

    [Fact]
    public async Task FetchAsync_MapsIsReadFalseToDto()
    {
        // Arrange
        var responseJson = BuildMailResponse(new FakeEmail("mail-1", "Subject", "Normal", "a@b.com", "preview", "https://link", IsRead: false));
        var provider = CreateProvider(new RecordingHttpMessageHandler(responseJson));

        // Act
        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsRead);
    }

    [Fact]
    public async Task FetchAsync_MapsIsReadTrueToDto()
    {
        // Arrange
        var responseJson = BuildMailResponse(new FakeEmail("mail-2", "Subject", "Normal", "a@b.com", "preview", "https://link", IsRead: true));
        var provider = CreateProvider(new RecordingHttpMessageHandler(responseJson));

        // Act
        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsRead);
    }

    private static GraphOutlookSourceProvider CreateProvider(RecordingHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        var factory = new StubGraphClientFactory(graphClient);
        return new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);
    }

    private static ConnectorExecutionRequest CreateRequest()
        => new(new CheckpointIdentity("outlook", "inbox", "acme"),
               DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

    private static string BuildMailResponse(params FakeEmail[] emails)
    {
        var items = emails.Select(e => new Dictionary<string, object?>
        {
            ["id"] = e.Id,
            ["subject"] = e.Subject,
            ["importance"] = e.Importance,
            ["sender"] = new Dictionary<string, object?>
            {
                ["emailAddress"] = new Dictionary<string, object?>
                {
                    ["address"] = e.SenderAddress
                }
            },
            ["bodyPreview"] = e.BodyPreview,
            ["webLink"] = e.WebLink,
            ["receivedDateTime"] = DateTimeOffset.UtcNow.ToString("o"),
            ["conversationId"] = $"conv-{e.Id}",
            ["isRead"] = e.IsRead
        });

        return JsonSerializer.Serialize(new { value = items });
    }

    private sealed record FakeEmail(
        string Id,
        string Subject,
        string Importance,
        string SenderAddress,
        string BodyPreview,
        string WebLink,
        bool IsRead);
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly HttpStatusCode _statusCode;

    public RecordingHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    public HttpRequestMessage? LastRequest { get; private set; }
    public Uri? LastRequestUri => LastRequest?.RequestUri;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

internal sealed class StubGraphClientFactory : IGraphClientFactory
{
    private readonly GraphServiceClient _client;

    public StubGraphClientFactory(GraphServiceClient client)
    {
        _client = client;
    }

    public Task<GraphServiceClient> CreateClientAsync(string userOid, CancellationToken ct)
        => Task.FromResult(_client);
}
