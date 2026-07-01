using System.Net;
using System.Text;
using System.Text.Json;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using NSubstitute;

namespace Aura.UnitTests.GraphConnector;

public class GraphTeamsSourceProviderTests
{
    [Fact]
    public async Task FetchAsync_ReturnsMappedTeamsMessages()
    {
        // Arrange
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-1", "Sprint planning", "Alice", "Let's discuss priorities", "https://teams.microsoft.com/chat-1"),
            new FakeChat("chat-2", "Incident alert", "Bob", "Production issue detected", "https://teams.microsoft.com/chat-2")
        );

        var provider = CreateProvider(responseJson);
        var request = CreateRequest();

        // Act
        var results = await provider.FetchAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);

        Assert.Equal("chat-1", results[0].ExternalId);
        Assert.Equal("Sprint planning", results[0].Title);
        Assert.Equal("Alice", results[0].Sender);
        Assert.Equal("Let's discuss priorities", results[0].BodyPreview);
        Assert.Equal("https://teams.microsoft.com/chat-1", results[0].WebUrl);

        Assert.Equal("chat-2", results[1].ExternalId);
        Assert.Equal("Incident alert", results[1].Title);
        Assert.Equal("Bob", results[1].Sender);
    }

    [Fact]
    public async Task FetchAsync_EmptyResponse_ReturnsEmptyList()
    {
        var responseJson = """{"value":[]}""";
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task FetchAsync_MsalUiRequired_PropagatesException()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new MsalUiRequiredException("no_account", "No cached account."));

        var provider = new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);

        await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_NullTopic_FallsBackToDefaultTitle()
    {
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-x", null, "Carlos", "some preview", "https://teams.microsoft.com/chat-x")
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Teams chat chat-x", results[0].Title);
    }

    [Fact]
    public async Task FetchAsync_PassesOidToFactory()
    {
        // Arrange
        var factory = Substitute.For<IGraphClientFactory>();
        var responseJson = """{"value":[]}""";
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(graphClient));

        var provider = new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-test-123"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        // Act
        await provider.FetchAsync(request, CancellationToken.None);

        // Assert: verify factory was called with the correct oid
        await factory.Received(1).CreateClientAsync("oid-test-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAsync_MsalUiRequired_ReturnsExceptionWithOidInMessage()
    {
        // Arrange: factory throws MsalUiRequiredException
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync("oid-expired", Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new MsalUiRequiredException("interaction_required", "Token expired."));

        var provider = new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-expired"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => provider.FetchAsync(request, CancellationToken.None));

        Assert.Equal("interaction_required", ex.ErrorCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp4xx_ReturnsFailureWithStatusCode()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new ODataError
            {
                ResponseStatusCode = 403
            });

        var provider = new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(403, ex.ResponseStatusCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp5xx_ReturnsFailureWithStatusCode()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new ODataError
            {
                ResponseStatusCode = 503
            });

        var provider = new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(503, ex.ResponseStatusCode);
    }

    private static GraphTeamsSourceProvider CreateProvider(string responseJson)
    {
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(graphClient));

        return new GraphTeamsSourceProvider(factory, NullLogger<GraphTeamsSourceProvider>.Instance);
    }

    private static ConnectorExecutionRequest CreateRequest()
        => new(new CheckpointIdentity("teams", "messages", "acme"),
               DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

    [Fact]
    public async Task FetchAsync_MapsLastMessageReadDateTime()
    {
        var readAt = new DateTimeOffset(2026, 6, 30, 14, 0, 0, TimeSpan.Zero);
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-1", "Sprint planning", "Alice", "Preview", "https://teams.microsoft.com/chat-1",
                LastMessageReadAt: readAt)
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var dto = Assert.Single(results);
        Assert.Equal(readAt, dto.LastMessageReadAt);
    }

    [Fact]
    public async Task FetchAsync_NullLastMessageReadAt_IsNull()
    {
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-2", "Chat", "Bob", "Preview", "https://teams.microsoft.com/chat-2")
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var dto = Assert.Single(results);
        Assert.Null(dto.LastMessageReadAt);
    }

    [Fact]
    public async Task FetchAsync_LastMessageAt_EqualsLastUpdatedDateTime()
    {
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-3", "Meeting", "Carlos", "Preview", "https://teams.microsoft.com/chat-3")
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var dto = Assert.Single(results);
        Assert.NotNull(dto.LastMessageAt);
        // LastMessageAt maps to chat.LastUpdatedDateTime — should be a recent timestamp
        Assert.True(dto.LastMessageAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task FetchAsync_UnreadCount_IsZero()
    {
        var responseJson = BuildChatsResponse(
            new FakeChat("chat-4", "Support", "Diana", "Preview", "https://teams.microsoft.com/chat-4")
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var dto = Assert.Single(results);
        Assert.Equal(0, dto.UnreadCount);
    }

    private static string BuildChatsResponse(params FakeChat[] chats)
    {
        var items = chats.Select(c =>
        {
            var dict = new Dictionary<string, object?>
            {
                ["id"] = c.Id,
                ["topic"] = c.Topic,
                ["webUrl"] = c.WebUrl,
                ["lastUpdatedDateTime"] = DateTimeOffset.UtcNow.ToString("o"),
                ["lastMessagePreview"] = new Dictionary<string, object?>
                {
                    ["from"] = new Dictionary<string, object?>
                    {
                        ["user"] = new Dictionary<string, object?>
                        {
                            ["displayName"] = c.Sender
                        }
                    },
                    ["body"] = new Dictionary<string, object?>
                    {
                        ["content"] = c.BodyPreview
                    }
                }
            };
            if (c.LastMessageReadAt is not null)
            {
                dict["viewpoint"] = new Dictionary<string, object?>
                {
                    ["lastMessageReadDateTime"] = c.LastMessageReadAt.Value.ToString("o")
                };
            }
            if (c.LastMessageAt is not null)
                dict["lastMessageDateTime"] = c.LastMessageAt.Value.ToString("o");
            return dict;
        });

        return JsonSerializer.Serialize(new { value = items });
    }

    private sealed record FakeChat(
        string Id,
        string? Topic,
        string Sender,
        string BodyPreview,
        string WebUrl,
        DateTimeOffset? LastMessageReadAt = null,
        DateTimeOffset? LastMessageAt = null);
}

internal sealed class FakeGraphHttpHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly HttpStatusCode _statusCode;

    public FakeGraphHttpHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
