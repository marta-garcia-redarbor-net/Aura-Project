using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aura.UI.Models;
using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

public sealed class CalendarApiClient : ICalendarApiClient
{
    private static readonly ActivitySource ActivitySource = new("Aura.UI.CalendarApi");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ILogger<CalendarApiClient> _logger;

    public CalendarApiClient(HttpClient httpClient, ILogger<CalendarApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UpcomingMeetingResponse>> GetUpcomingMeetingsAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("calendar.upcoming-meetings.read", ActivityKind.Client);
        activity?.SetTag("http.url", "/api/dashboard/upcoming-meetings");
        activity?.SetTag("http.method", "GET");

        try
        {
            using var response = await _httpClient.GetAsync("/api/dashboard/upcoming-meetings", cancellationToken);
            response.EnsureSuccessStatusCode();

            var meetings = await response.Content.ReadFromJsonAsync<UpcomingMeetingResponse[]>(SerializerOptions, cancellationToken);
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("calendar.meetings.count", meetings?.Length ?? 0);
            _logger.LogInformation("Fetched {Count} upcoming meetings", meetings?.Length ?? 0);
            return meetings ?? [];
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to fetch upcoming meetings");
            throw;
        }
    }

    public async Task<IReadOnlyList<UpcomingMeetingResponse>> GetTodayCalendarAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("calendar.today.read", ActivityKind.Client);
        activity?.SetTag("http.url", "/api/dashboard/today-calendar");
        activity?.SetTag("http.method", "GET");

        try
        {
            using var response = await _httpClient.GetAsync("/api/dashboard/today-calendar", cancellationToken);
            response.EnsureSuccessStatusCode();

            var meetings = await response.Content.ReadFromJsonAsync<UpcomingMeetingResponse[]>(SerializerOptions, cancellationToken);
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("calendar.meetings.count", meetings?.Length ?? 0);
            _logger.LogInformation("Fetched {Count} today calendar events", meetings?.Length ?? 0);
            return meetings ?? [];
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to fetch today calendar");
            throw;
        }
    }
}
