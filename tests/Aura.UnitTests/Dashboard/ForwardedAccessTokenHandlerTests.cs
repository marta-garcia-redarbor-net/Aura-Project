using System.Net;
using Aura.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Unit tests for <see cref="ForwardedAccessTokenHandler"/>.
/// Verifies that the handler correctly forwards Authorization headers from
/// the incoming HTTP context to outbound API requests.
/// </summary>
public class ForwardedAccessTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_WithBearerToken_ForwardsAuthorizationHeader()
    {
        var httpContextAccessor = CreateHttpContextAccessor("Bearer test-token-123");
        using var invoker = CreateInvoker(httpContextAccessor, out var innerHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.aura.test/api/dashboard/initial");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(innerHandler.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", innerHandler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal("test-token-123", innerHandler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithoutAuthorizationHeader_DoesNotSetAuthorization()
    {
        var httpContextAccessor = CreateHttpContextAccessor(authorizationHeader: null);
        using var invoker = CreateInvoker(httpContextAccessor, out var innerHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.aura.test/api/dashboard/initial");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(innerHandler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithEmptyAuthorizationHeader_DoesNotSetAuthorization()
    {
        var httpContextAccessor = CreateHttpContextAccessor(authorizationHeader: "");
        using var invoker = CreateInvoker(httpContextAccessor, out var innerHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.aura.test/api/dashboard/initial");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(innerHandler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithNullHttpContext_DoesNotSetAuthorization()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        using var invoker = CreateInvoker(httpContextAccessor, out var innerHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.aura.test/api/dashboard/initial");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(innerHandler.LastRequest?.Headers.Authorization);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? authorizationHeader)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddAuthentication("Cookies")
            .AddCookie()
            .Services
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        if (authorizationHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }

    private static HttpMessageInvoker CreateInvoker(
        IHttpContextAccessor httpContextAccessor,
        out CapturingInnerHandler innerHandler)
    {
        innerHandler = new CapturingInnerHandler();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "test-client-id"
            })
            .Build();

        var logger = Substitute.For<ILogger<ForwardedAccessTokenHandler>>();

        var handler = new ForwardedAccessTokenHandler(httpContextAccessor, configuration, logger)
        {
            InnerHandler = innerHandler
        };
        return new HttpMessageInvoker(handler);
    }

    /// <summary>
    /// Captures the outbound request so tests can inspect headers set by the delegating handler.
    /// </summary>
    private sealed class CapturingInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
