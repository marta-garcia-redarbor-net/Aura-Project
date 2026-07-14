using System.Net;
using System.Net.Http;

namespace Aura.E2E.Browser;

public sealed class PlaywrightHostReachabilityGateTests
{
    [Fact]
    public async Task EnsureHostReachableAsync_WhenHealthProbeReturnsNonSuccess_ThrowsHostNotReachableWithUrlAndPort()
    {
        var baseUrl = "http://127.0.0.1:5099";
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("internal server error")
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PlaywrightWebApplicationFactory.EnsureHostReachableAsync(baseUrl, handler));

        Assert.Contains("HostNotReachable:", exception.Message, StringComparison.Ordinal);
        Assert.Contains(baseUrl, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"{baseUrl}/", exception.Message, StringComparison.Ordinal);
        Assert.Contains("500", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureHostReachableAsync_WhenProbeThrowsTransportException_ThrowsHostNotReachableWithInnerError()
    {
        var baseUrl = "http://127.0.0.1:5100";
        using var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PlaywrightWebApplicationFactory.EnsureHostReachableAsync(baseUrl, handler));

        Assert.Contains("HostNotReachable:", exception.Message, StringComparison.Ordinal);
        Assert.Contains(baseUrl, exception.Message, StringComparison.Ordinal);
        Assert.Contains("connection refused", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task EnsureHostReachableAsync_WhenHealthProbeReturnsSuccess_DoesNotThrow()
    {
        var baseUrl = "http://127.0.0.1:5101";
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("healthy")
        });

        await PlaywrightWebApplicationFactory.EnsureHostReachableAsync(baseUrl, handler);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
