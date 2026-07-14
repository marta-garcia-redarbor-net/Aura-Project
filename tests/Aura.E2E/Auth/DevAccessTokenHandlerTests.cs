using System.Net;
using System.Net.Http.Headers;
using Aura.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.E2E.Auth;

/// <summary>
/// Tests for <see cref="DevAccessTokenHandler"/>.
/// Validates token-skip behavior when auth header already present,
/// and graceful fallback when the mock-login endpoint is unreachable.
/// </summary>
public class DevAccessTokenHandlerTests
{
    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task SendAsync_WithExistingAuth_DoesNotOverwrite()
    {
        // Arrange
        var config = BuildConfig("http://localhost:5180");
        var handler = new DevAccessTokenHandler(
            new StubHttpClientFactory(),
            config,
            NullLogger<DevAccessTokenHandler>.Instance)
        {
            InnerHandler = new CapturingHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "existing-user-token");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert — the existing token must be preserved, not overwritten
        Assert.Equal("existing-user-token",
            ((CapturingHandler)handler.InnerHandler).LastRequest!.Headers.Authorization!.Parameter);
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task GetOrAcquireTokenAsync_WhenApiUnreachable_ReturnsNull()
    {
        // Arrange — unreachable endpoint, handler should log warning and return null
        var config = BuildConfig("http://not-reachable:9999");
        var handler = new DevAccessTokenHandler(
            new StubHttpClientFactory(),
            config,
            NullLogger<DevAccessTokenHandler>.Instance);

        // Act
        var token = await handler.GetOrAcquireTokenAsync(CancellationToken.None);

        // Assert — graceful fallback, no exception thrown
        Assert.Null(token);
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task SendAsync_WhenApiUnreachable_SendsRequestWithoutAuth()
    {
        // Arrange
        var config = BuildConfig("http://not-reachable:9999");
        var capturingHandler = new CapturingHandler();
        var handler = new DevAccessTokenHandler(
            new StubHttpClientFactory(),
            config,
            NullLogger<DevAccessTokenHandler>.Instance)
        {
            InnerHandler = capturingHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert — request went through, just without auth
        Assert.NotNull(capturingHandler.LastRequest);
        Assert.Null(capturingHandler.LastRequest!.Headers.Authorization);
    }

    private static IConfiguration BuildConfig(string baseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuraApi:BaseUrl"] = baseUrl
            })
            .Build();

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
