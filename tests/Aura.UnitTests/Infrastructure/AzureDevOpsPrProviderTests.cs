using System.Net;
using System.Text;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.AzureDevOps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.UnitTests.Infrastructure;

public class AzureDevOpsPrProviderTests
{
    [Fact]
    public async Task FetchAsync_WithReviewerId_MapsReviewerIdentityOid()
    {
        var payload = """
        {
          "value": [
            {
              "pullRequestId": 42,
              "title": "PR with reviewer id",
              "creationDate": "2026-07-09T10:00:00Z",
              "status": "active",
              "isDraft": false,
              "createdBy": { "displayName": "Author" },
              "reviewers": [
                { "id": "user-abc", "displayName": "Jane Doe", "isContainer": false }
              ]
            }
          ]
        }
        """;

        var provider = CreateProvider(payload);

        var result = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var reviewer = Assert.Single(Assert.Single(result).ReviewerIdentities!);
        Assert.Equal("user-abc", reviewer.Oid);
        Assert.False(reviewer.IsContainer);
    }

    [Fact]
    public async Task FetchAsync_WithoutReviewerId_MapsNullOidAndPreservesDisplayName()
    {
        var payload = """
        {
          "value": [
            {
              "pullRequestId": 43,
              "title": "PR without reviewer id",
              "creationDate": "2026-07-09T10:00:00Z",
              "status": "active",
              "isDraft": false,
              "createdBy": { "displayName": "Author" },
              "reviewers": [
                { "displayName": "Jane Doe", "isContainer": false }
              ]
            }
          ]
        }
        """;

        var provider = CreateProvider(payload);

        var result = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var reviewer = Assert.Single(Assert.Single(result).ReviewerIdentities!);
        Assert.Null(reviewer.Oid);
        Assert.Equal("Jane Doe", reviewer.DisplayName);
    }

    [Fact]
    public async Task FetchAsync_WithContainerReviewer_MapsIsContainerTrue()
    {
        var payload = """
        {
          "value": [
            {
              "pullRequestId": 44,
              "title": "PR with group reviewer",
              "creationDate": "2026-07-09T10:00:00Z",
              "status": "active",
              "isDraft": false,
              "createdBy": { "displayName": "Author" },
              "reviewers": [
                { "id": "group-xyz", "displayName": "Backend Team", "isContainer": true }
              ]
            }
          ]
        }
        """;

        var provider = CreateProvider(payload);

        var result = await provider.FetchAsync(CreateRequest(), CancellationToken.None);

        var reviewer = Assert.Single(Assert.Single(result).ReviewerIdentities!);
        Assert.Equal("group-xyz", reviewer.Oid);
        Assert.True(reviewer.IsContainer);
    }

    private static AzureDevOpsPrProvider CreateProvider(string jsonPayload)
    {
        var handler = new StubHttpMessageHandler(jsonPayload);
        var httpClient = new HttpClient(handler);
        var options = new AzureDevOpsPrOptions
        {
            Enabled = true,
            PatToken = "pat-token",
            Organization = "acme-org",
            Project = "Aura"
        };

        return new AzureDevOpsPrProvider(httpClient, NullLogger<AzureDevOpsPrProvider>.Instance, options);
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("pr", "azdo", "acme"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow);

    private sealed class StubHttpMessageHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
