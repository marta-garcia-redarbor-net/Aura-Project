using Aura.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Aura.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SetsXContentTypeOptionsNosniff()
    {
        var env = CreateEnvironment("Production");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }

    [Fact]
    public async Task InvokeAsync_SetsXFrameOptionsDeny()
    {
        var env = CreateEnvironment("Production");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
    }

    [Fact]
    public async Task InvokeAsync_SetsContentSecurityPolicy()
    {
        var env = CreateEnvironment("Production");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("default-src 'self'", context.Response.Headers["Content-Security-Policy"]);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_SetsHstsHeader()
    {
        var env = CreateEnvironment("Production");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("max-age=31536000; includeSubDomains",
            context.Response.Headers["Strict-Transport-Security"]);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_DoesNotSetHstsHeader()
    {
        var env = CreateEnvironment("Development");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"),
            "HSTS header should NOT be present in Development environment");
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_StillSetsOtherHeaders()
    {
        var env = CreateEnvironment("Development");
        var middleware = new SecurityHeadersMiddleware(next: _ => Task.CompletedTask, env);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("default-src 'self'", context.Response.Headers["Content-Security-Policy"]);
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);
        return env;
    }
}
