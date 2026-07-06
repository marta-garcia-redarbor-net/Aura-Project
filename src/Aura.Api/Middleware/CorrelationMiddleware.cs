using System.Diagnostics;

namespace Aura.Api.Middleware;

/// <summary>
/// ASP.NET Core middleware that ensures every request has a correlation ID.
/// Reads the X-Correlation-Id request header or generates a new GUID,
/// sets HttpContext.TraceIdentifier, opens an ILogger.BeginScope with the
/// correlation ID, logs entry and exit with method/path/status/ms, and
/// includes the correlation ID in the response header.
/// </summary>
public sealed partial class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes the middleware with the next delegate in the pipeline.
    /// </summary>
    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Processes the request: resolves the correlation ID, sets scope,
    /// logs entry/exit, and invokes the next middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationMiddleware> logger)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;

        using var _ = logger.BeginScope("{CorrelationId}", correlationId);
        var stopwatch = Stopwatch.StartNew();

        // Set the response header before the next middleware so it's available
        // even if the response starts (e.g., auth challenge) or an exception occurs.
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        Log.RequestStarted(logger, context.Request.Method, context.Request.Path);

        try
        {
            await _next(context);
            stopwatch.Stop();

            Log.RequestCompleted(logger,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.RequestFailed(logger, ex,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var header = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        return Guid.NewGuid().ToString();
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
            Message = "Request started {Method} {Path}")]
        public static partial void RequestStarted(ILogger logger, string method, string? path);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
            Message = "Request completed {Method} {Path} with {StatusCode} in {ElapsedMilliseconds}ms")]
        public static partial void RequestCompleted(ILogger logger, string method, string? path, int statusCode, long elapsedMilliseconds);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Error,
            Message = "Request failed {Method} {Path} after {ElapsedMilliseconds}ms")]
        public static partial void RequestFailed(ILogger logger, Exception exception, string method, string? path, long elapsedMilliseconds);
    }
}
