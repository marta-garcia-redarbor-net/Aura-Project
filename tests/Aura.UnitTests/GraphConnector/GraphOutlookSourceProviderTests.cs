using System.Net;
using System.Text.Json;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
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
        factory.CreateClientAsync(Arg.Any<CancellationToken>())
            .Returns<GraphServiceClient>(x => throw new MsalUiRequiredException("no_account", "Re-auth needed."));

        var provider = new GraphOutlookSourceProvider(factory, NullLogger<GraphOutlookSourceProvider>.Instance);

        await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => provider.FetchAsync(CreateRequest(), CancellationToken.None));
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

    private static GraphOutlookSourceProvider CreateProvider(string responseJson)
    {
        var handler = new FakeGraphHttpHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
        var adapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var graphClient = new GraphServiceClient(adapter);

        var factory = Substitute.For<IGraphClientFactory>();
        factory.CreateClientAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(graphClient));

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
