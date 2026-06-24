using Aura.Api.Endpoints;
using Aura.Application;
using Aura.Infrastructure;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuraApplication();
builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Aura.Api.DashboardPipeline");

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var isDashboardRequest = context.Request.Path.StartsWithSegments("/api/dashboard", StringComparison.OrdinalIgnoreCase);
    if (!isDashboardRequest)
    {
        await next();
        return;
    }

    var stopwatch = Stopwatch.StartNew();
    using var activity = new Activity("dashboard.request").Start();
    activity.SetTag("http.route", context.Request.Path.Value);
    activity.SetTag("http.method", context.Request.Method);

    try
    {
        await next();
        activity.SetTag("http.status_code", context.Response.StatusCode);
        Aura.Api.DashboardRequestLog.RequestCompleted(
            logger,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        Aura.Api.DashboardRequestLog.RequestFailed(
            logger,
            ex,
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
        throw;
    }
});

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapAuthEndpoints(app.Environment);
app.MapDashboardEndpoints();
app.MapGraphConnectorEndpoints();
app.MapSyncEndpoints();

app.Run();

namespace Aura.Api
{
    /// <summary>
    /// Marker type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
    /// Lives in the Api assembly so the factory can locate the entry point.
    /// </summary>
    public sealed class ApiMarker;

    internal static partial class DashboardRequestLog
    {
        [LoggerMessage(EventId = 1101, Level = LogLevel.Information,
            Message = "Dashboard request {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds}ms")]
        public static partial void RequestCompleted(
            ILogger logger,
            string method,
            string? path,
            int statusCode,
            long elapsedMilliseconds);

        [LoggerMessage(EventId = 1102, Level = LogLevel.Error,
            Message = "Dashboard request {Method} {Path} failed after {ElapsedMilliseconds}ms")]
        public static partial void RequestFailed(
            ILogger logger,
            Exception exception,
            string method,
            string? path,
            long elapsedMilliseconds);
    }
}
