using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

internal sealed partial class CalendarConnectorAdapter : IConnectorAdapter
{
    private static readonly Func<IReadOnlyList<CalendarEventDto>> DefaultFixtureProvider = LoadDefaultFixtures;

    private readonly ILogger<CalendarConnectorAdapter> _logger;
    private readonly ICalendarEventStore _store;
    private readonly CalendarEventMapper _mapper;
    private readonly Func<IReadOnlyList<CalendarEventDto>> _fixtureProvider;
    private readonly IMessageSourceProvider<CalendarEventDto>? _sourceProvider;

    public CalendarConnectorAdapter(
        ILogger<CalendarConnectorAdapter> logger,
        ICalendarEventStore store,
        CalendarEventMapper mapper,
        Func<IReadOnlyList<CalendarEventDto>>? fixtureProvider = null,
        IMessageSourceProvider<CalendarEventDto>? sourceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(mapper);
        _logger = logger;
        _store = store;
        _mapper = mapper;
        _fixtureProvider = fixtureProvider ?? DefaultFixtureProvider;
        _sourceProvider = sourceProvider;
    }

    public string ConnectorName => "calendar";

    public async Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<CalendarEventDto> payloads;

        if (_sourceProvider is not null)
        {
            try
            {
                payloads = await _sourceProvider.FetchAsync(request, ct);
            }
            catch (Exception ex)
            {
                Log.CalendarProviderError(_logger, ex);
                return new ConnectorExecutionResult(
                    request.Identity,
                    0,
                    ConnectorExecutionStatus.Failure,
                    $"Calendar provider failed: {ex.Message}",
                    MaxProcessedAt: request.WindowEnd);
            }
        }
        else
        {
            payloads = _fixtureProvider();
        }

        var mappedCount = 0;
        var skippedCount = 0;

        foreach (var payload in payloads)
        {
            if (_mapper.TryMap(payload, out var calendarEvent) && calendarEvent is not null)
            {
                await _store.SaveAsync(calendarEvent, ct);
                mappedCount++;
                continue;
            }

            skippedCount++;
            Log.CalendarEventSkipped(_logger, payload.ExternalId ?? "<missing>");
        }

        var status = skippedCount > 0 ? ConnectorExecutionStatus.PartialFailure : ConnectorExecutionStatus.Success;
        var failureReason = skippedCount > 0 ? $"Skipped {skippedCount} invalid calendar payload(s)." : null;

        Log.CalendarExecutionMapped(_logger, request.Identity.Source, request.Identity.Tenant, request.WindowStart, request.WindowEnd, mappedCount, skippedCount);

        return new ConnectorExecutionResult(
            request.Identity,
            mappedCount,
            status,
            failureReason,
            MaxProcessedAt: request.WindowEnd);
    }

    private static IReadOnlyList<CalendarEventDto> LoadDefaultFixtures()
        =>
        [
            new CalendarEventDto
            {
                ExternalId = "cal-1001",
                Subject = "Sprint planning",
                Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = true,
                JoinUrl = "https://teams.microsoft.com/l/meetup-join/1001",
                OrganizerName = "Alice",
                OrganizerAddress = "alice@example.com",
                LocationDisplayName = "Conference Room A",
                IsCancelled = false,
                OriginalTimeZone = "UTC"
            },
            new CalendarEventDto
            {
                ExternalId = null,
                Subject = "Missing id should be skipped",
                Start = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false
            },
            new CalendarEventDto
            {
                ExternalId = "cal-1003",
                Subject = "Cancelled meeting",
                Start = new DateTimeOffset(2026, 6, 24, 16, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 6, 24, 17, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting = false,
                IsCancelled = true
            }
        ];

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3501,
            Level = LogLevel.Information,
            Message = "Calendar connector adapter executed for source {Source}, tenant {Tenant}, window {WindowStart} → {WindowEnd}, mapped {MappedCount}, skipped {SkippedCount}")]
        public static partial void CalendarExecutionMapped(
            ILogger logger,
            string source,
            string tenant,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            int mappedCount,
            int skippedCount);

        [LoggerMessage(
            EventId = 3502,
            Level = LogLevel.Warning,
            Message = "Calendar event skipped because required fields were missing. ExternalId={ExternalId}")]
        public static partial void CalendarEventSkipped(
            ILogger logger,
            string externalId);

        [LoggerMessage(
            EventId = 3503,
            Level = LogLevel.Error,
            Message = "Calendar provider failed with exception")]
        public static partial void CalendarProviderError(
            ILogger logger,
            Exception exception);
    }
}