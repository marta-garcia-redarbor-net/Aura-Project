using Aura.Api.Adapters;
using Aura.Api.Endpoints;
using Aura.Api.Hubs;
using Aura.Api.Middleware;
using Aura.Api.Validation;
using Aura.Api.Workers;
using Aura.Application;
using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Infrastructure;
using FluentValidation;
using System.Diagnostics;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuraApplication();
builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddSignalR();
builder.Services.AddScoped<GetUpcomingMeetingsUseCase>();
builder.Services.AddValidatorsFromAssembly(typeof(Aura.Api.ApiMarker).Assembly);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Meeting alert dispatcher (uses unified AlertHub)
builder.Services.AddSingleton<IMeetingAlertDispatcher, SignalRMeetingAlertDispatcher>();

// Work item notification pipeline
builder.Services.AddSingleton<IWorkItemNotificationDispatcher, SignalRWorkItemNotificationDispatcher>();

// Background workers
builder.Services.AddHostedService<WorkItemNotificationWorker>();
builder.Services.AddHostedService<MeetingAlertWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUiOrigin", policy =>
    {
        var uiOrigin = builder.Configuration["Cors:UiOrigin"] ?? "http://localhost:5190";
        policy.WithOrigins(uiOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate limiting — sliding window per client IP
builder.Services.AddRateLimiter(options =>
{
    var defaultLimit = builder.Configuration.GetValue<int>("RateLimiting:Default:PermitLimit", 100);
    var defaultWindow = builder.Configuration.GetValue<int>("RateLimiting:Default:WindowSeconds", 60);
    var authLimit = builder.Configuration.GetValue<int>("RateLimiting:Auth:PermitLimit", 10);
    var authWindow = builder.Configuration.GetValue<int>("RateLimiting:Auth:WindowSeconds", 60);

    // Global limiter — applies to all endpoints unless overridden
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = defaultLimit,
                Window = TimeSpan.FromSeconds(defaultWindow),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            });
    });

    // Named policy for auth endpoints — stricter limits
    options.AddPolicy("auth", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = authLimit,
                Window = TimeSpan.FromSeconds(authWindow),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("0");
        }
        else
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";
        }
        return ValueTask.CompletedTask;
    };
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Aura.Api.DashboardPipeline");
var errorStore = app.Services.GetRequiredService<IErrorStore>();

app.UseRateLimiter();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("AllowUiOrigin");
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

        if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            await errorStore.RecordAsync(
                new ErrorEntry(
                    context.TraceIdentifier,
                    DateTimeOffset.UtcNow,
                    $"{context.Request.Method} {context.Request.Path}: HTTP {context.Response.StatusCode}"),
                context.RequestAborted);
        }

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
        await errorStore.RecordAsync(
            new ErrorEntry(
                context.TraceIdentifier,
                DateTimeOffset.UtcNow,
                $"{context.Request.Method} {context.Request.Path}: {ex.Message}"),
            context.RequestAborted);

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
app.MapFocusStateEndpoints();
app.MapGraphConnectorEndpoints();
app.MapSyncEndpoints();
app.MapTriageEndpoints();
app.MapWorkItemsEndpoints();
app.MapHub<AlertHub>("/hubs/alerts");

if (app.Environment.IsDevelopment())
{
    app.MapDebugEndpoints();
}

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
