using System.Net;
using System.Text.Json;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.UnitTests.TestDoubles.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using NSubstitute;

namespace Aura.UnitTests.GraphConnector;

public class GraphOutlookSourceProviderTests
{
    [Fact]
    public async Task FetchAsync_ReturnsMappedOutlookEmails()
    {
        var responseJson = BuildMailResponse(
            new FakeEmail("mail-1", "Sprint review", "High", "alice@acme.dev", "Review the sprint", "https://outlook.office.com/mail/1"),
            new FakeEmail("mail-2", "Budget approval", "Normal", "bob@acme.dev", "Please approve", "https://outlook.office.com/mail/2")
        );

        var provider = CreateProvider(responseJson);
        var request = CreateRequest();

        var results = await provider.FetchAsync(request, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("mail-1", results[0].ExternalId);
        Assert.Equal("Sprint review", results[0].Subject);
        Assert.Equal("High", results[0].Importance);
        Assert.Equal("alice@acme.dev", results[0].SenderAddress);
        Assert.Equal("Review the sprint", results[0].BodyPreview);
        Assert.Equal("https://outlook.office.com/mail/1", results[0].WebLink);
    }

    [Fact]
    public async Task FetchAsync_EmptyMailbox_ReturnsEmptyList()
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
            .Returns<GraphServiceClient>(x => throw new MsalUiRequiredException("no_account", "Re-auth needed."));

        var provider = new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);

        await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_MsalUiRequired_EmitsWarningLogWithOidAndConnector_AndTokenExpiredMetric()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync("oid-outlook-expired", Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new MsalUiRequiredException("interaction_required", "Re-auth needed."));

        var logger = new ScopeAwareTestLogger<GraphOutlookSourceProvider>();
        var provider = new GraphOutlookSourceProvider(factory, logger);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("outlook", "inbox", "acme", userOid: "oid-outlook-expired"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.token.expired");

        await Assert.ThrowsAsync<MsalUiRequiredException>(() => provider.FetchAsync(request, CancellationToken.None));

        var warning = logger.Entries.Single(e => e.EventId.Id == 3307);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, warning.Level);
        Assert.Equal("oid-outlook-expired", warning.State["Oid"]?.ToString());
        Assert.Equal("outlook", warning.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.token.expired"
            && m.GetTag("connector") == "outlook"
            && m.GetTag("oid") == "oid-outlook-expired"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("outlook", metric.GetTag("connector"));
        Assert.Equal("oid-outlook-expired", metric.GetTag("oid"));
    }

    [Fact]
    public async Task FetchAsync_MapsWebLinkToDto()
    {
        var responseJson = BuildMailResponse(
            new FakeEmail("mail-x", "Update", "Normal", "dev@acme.dev", "quick note", "https://outlook.office.com/mail/x")
        );
        var provider = CreateProvider(responseJson);

        var results = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("https://outlook.office.com/mail/x", results[0].WebLink);
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

        var provider = new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);
        var request = new ConnectorExecutionRequest(
            new CheckpointIdentity("outlook", "inbox", "acme", userOid: "oid-outlook-42"),
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        // Act
        await provider.FetchAsync(request, CancellationToken.None);

        // Assert
        await factory.Received(1).CreateClientAsync("oid-outlook-42", Arg.Any<CancellationToken>());
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

        var provider = new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(403, ex.ResponseStatusCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp4xx_EmitsWarningLogAndHttpErrorMetricWithConnectorAndEndpoint()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new ODataError { ResponseStatusCode = 403 });

        var logger = new ScopeAwareTestLogger<GraphOutlookSourceProvider>();
        var provider = new GraphOutlookSourceProvider(factory, logger);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.http.error");

        var ex = await Assert.ThrowsAsync<ODataError>(() => provider.FetchAsync(CreateRequest(), CancellationToken.None));
        Assert.Equal(403, ex.ResponseStatusCode);

        var warning = logger.Entries.Single(e => e.EventId.Id == 3308);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, warning.Level);
        Assert.Equal("403", warning.State["StatusCode"]?.ToString());
        Assert.Equal("me/mailFolders/inbox/messages", warning.State["Endpoint"]?.ToString());
        Assert.Equal("outlook", warning.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.http.error"
            && m.GetTag("connector") == "outlook"
            && m.GetTag("status_code") == "403"
            && m.GetTag("endpoint") == "me/mailFolders/inbox/messages"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("outlook", metric.GetTag("connector"));
        Assert.Equal("403", metric.GetTag("status_code"));
        Assert.Equal("me/mailFolders/inbox/messages", metric.GetTag("endpoint"));
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

        var provider = new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);

        var ex = await Assert.ThrowsAsync<ODataError>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(503, ex.ResponseStatusCode);
    }

    [Fact]
    public async Task FetchAsync_GraphHttp5xx_EmitsErrorLogAndHttpErrorMetricWithConnectorAndEndpoint()
    {
        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(_ => throw new ODataError { ResponseStatusCode = 503 });

        var logger = new ScopeAwareTestLogger<GraphOutlookSourceProvider>();
        var provider = new GraphOutlookSourceProvider(factory, logger);

        using var meter = new MeterCapture("Aura.Infrastructure.GraphConnector", "graph.http.error");

        var ex = await Assert.ThrowsAsync<ODataError>(() => provider.FetchAsync(CreateRequest(), CancellationToken.None));
        Assert.Equal(503, ex.ResponseStatusCode);

        var error = logger.Entries.Single(e => e.EventId.Id == 3309);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, error.Level);
        Assert.Equal("503", error.State["StatusCode"]?.ToString());
        Assert.Equal("me/mailFolders/inbox/messages", error.State["Endpoint"]?.ToString());
        Assert.Equal("outlook", error.State["Connector"]?.ToString());

        var metric = Assert.Single(meter.Snapshot().Where(m =>
            m.Instrument == "graph.http.error"
            && m.GetTag("connector") == "outlook"
            && m.GetTag("status_code") == "503"
            && m.GetTag("endpoint") == "me/mailFolders/inbox/messages"));
        Assert.Equal(1L, metric.Value);
        Assert.Equal("outlook", metric.GetTag("connector"));
        Assert.Equal("503", metric.GetTag("status_code"));
        Assert.Equal("me/mailFolders/inbox/messages", metric.GetTag("endpoint"));
    }

    private static GraphOutlookSourceProvider CreateProvider(string responseJson)
    {
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(graphClient));

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
            ["conversationId"] = $"conv-{e.Id}"
        });

        return JsonSerializer.Serialize(new { value = items });
    }

    private sealed record FakeEmail(string Id, string Subject, string Importance, string SenderAddress, string BodyPreview, string WebLink);
}
