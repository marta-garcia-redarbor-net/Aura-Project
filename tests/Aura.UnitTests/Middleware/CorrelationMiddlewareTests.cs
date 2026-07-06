using System.Net;
using Aura.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Aura.UnitTests.TestDoubles.Observability;

namespace Aura.UnitTests.Middleware;

public class CorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenHeaderPresent_SetsTraceIdentifierAndResponseHeader()
    {
        var logger = Substitute.For<ILogger<CorrelationMiddleware>>();
        var middleware = new CorrelationMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "abc-123";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        Assert.Equal("abc-123", context.TraceIdentifier);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
        Assert.Equal("abc-123", context.Response.Headers["X-Correlation-Id"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_GeneratesNewGuid()
    {
        var logger = Substitute.For<ILogger<CorrelationMiddleware>>();
        var middleware = new CorrelationMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        Assert.NotNull(context.TraceIdentifier);
        Assert.NotEmpty(context.TraceIdentifier);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
        Assert.Equal(context.TraceIdentifier, context.Response.Headers["X-Correlation-Id"]);
    }

    [Fact]
    public async Task InvokeAsync_GeneratedId_IsValidGuid()
    {
        var logger = Substitute.For<ILogger<CorrelationMiddleware>>();
        var middleware = new CorrelationMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
        Assert.True(Guid.TryParse(correlationId, out _), "Should be a valid GUID");
    }

    [Fact]
    public async Task InvokeAsync_SetsCorrelationIdBeforeCallingNext()
    {
        string? correlationIdInNext = null;
        var logger = Substitute.For<ILogger<CorrelationMiddleware>>();
        var middleware = new CorrelationMiddleware(next: ctx =>
        {
            correlationIdInNext = ctx.TraceIdentifier;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "set-before-next";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        Assert.Equal("set-before-next", correlationIdInNext);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestCompletes_LogsEntryAndExitWithDurationAndStatus()
    {
        var logger = new ScopeAwareTestLogger<CorrelationMiddleware>();
        var middleware = new CorrelationMiddleware(next: ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/health";
        context.Request.Headers["X-Correlation-Id"] = "corr-entry-exit";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        var started = logger.Entries.Single(e => e.EventId.Id == 2001);
        var completed = logger.Entries.Single(e => e.EventId.Id == 2002);

        Assert.Equal(LogLevel.Information, started.Level);
        Assert.Equal("GET", started.State["Method"]?.ToString());
        Assert.Equal("/health", started.State["Path"]?.ToString());

        Assert.Equal(LogLevel.Information, completed.Level);
        Assert.Equal("GET", completed.State["Method"]?.ToString());
        Assert.Equal("/health", completed.State["Path"]?.ToString());
        Assert.Equal("200", completed.State["StatusCode"]?.ToString());

        var elapsedMilliseconds = Convert.ToInt64(completed.State["ElapsedMilliseconds"]);
        Assert.True(elapsedMilliseconds >= 0);
        Assert.Equal("corr-entry-exit", completed.Scope["CorrelationId"]?.ToString());
    }

    [Fact]
    public async Task InvokeAsync_EnrichesDownstreamLogsWithCorrelationIdScope()
    {
        var logger = new ScopeAwareTestLogger<CorrelationMiddleware>();
        var middleware = new CorrelationMiddleware(next: ctx =>
        {
            logger.LogInformation("Downstream endpoint reached");
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "corr-downstream-123";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, logger);

        var downstreamEntry = logger.Entries.Single(e => e.Message.Contains("Downstream endpoint reached", StringComparison.Ordinal));
        Assert.Equal("corr-downstream-123", downstreamEntry.Scope["CorrelationId"]?.ToString());
    }
}
