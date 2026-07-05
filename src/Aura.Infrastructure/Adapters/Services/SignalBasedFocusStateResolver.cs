using System.Diagnostics;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.FocusState;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Services;

/// <summary>
/// Signal-based implementation of <see cref="IFocusStateResolver"/>.
/// Resolves focus state using a priority chain:
/// (1) calendar meeting → <see cref="FocusStateType.Away"/>,
/// (2) blackout period → target state,
/// (3) outside working hours → <see cref="FocusStateType.Away"/>,
/// (4) fallback → <see cref="FocusStateType.WindowOfOpportunity"/>.
/// Stateless — creates a fresh <see cref="FocusState"/> per call.
/// </summary>
public sealed partial class SignalBasedFocusStateResolver : IFocusStateResolver
{
    private static readonly ActivitySource ActivitySource = new("Aura.Infrastructure.FocusState");

    private readonly ICalendarEventStore _calendarStore;
    private readonly IOptions<FocusStateOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SignalBasedFocusStateResolver> _logger;

    public SignalBasedFocusStateResolver(
        ICalendarEventStore calendarStore,
        IOptions<FocusStateOptions> options,
        TimeProvider timeProvider,
        ILogger<SignalBasedFocusStateResolver>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(calendarStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _calendarStore = calendarStore;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SignalBasedFocusStateResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId must not be null or whitespace.", nameof(userId));

        using var activity = ActivitySource.StartActivity("SignalBasedFocusStateResolver.ResolveAsync");
        activity?.SetTag("userId", userId);

        var utcNow = _timeProvider.GetUtcNow();
        var opts = _options.Value;
        var state = new FocusState();

        // Signal 1: Calendar meeting → Away
        var buffer = TimeSpan.FromMinutes(opts.MeetingBufferMinutes);
        var from = utcNow.Add(-buffer);
        var to = utcNow.Add(buffer);

        IReadOnlyList<CalendarEvent>? events = null;
        try
        {
            events = await _calendarStore.GetUpcomingAsync(from, to, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.CalendarStoreFailed(_logger, ex);
        }

        var userEvents = events?
            .Where(e => e.UserId is null || string.Equals(e.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (events is { Count: > 0 } && userEvents is { Count: 0 })
        {
            Log.NoCalendarStoreMatch(_logger, userId);
        }

        if (userEvents is { Count: > 0 })
        {
            state.GoToAway();
            activity?.SetTag("matched_signal", "calendar");
            activity?.SetTag("focus_state", "Away");
            Log.StateResolved(_logger, userId, "Away", "calendar");
            return state;
        }

        // Signal 2: Blackout period → target state
        foreach (var dto in opts.BlackoutPeriods)
        {
            try
            {
                var targetState = dto.TargetState switch
                {
                    "DeepWork" => FocusStateType.DeepWork,
                    "Away" => FocusStateType.Away,
                    _ => (FocusStateType?)null
                };

                if (targetState is null)
                    continue;

                var period = new BlackoutPeriod(
                    dto.Label,
                    targetState.Value,
                    dto.StartTime,
                    dto.EndTime,
                    dto.DaysOfWeek.AsReadOnly(),
                    dto.TimeZoneId);

                if (period.IsActive(utcNow, _timeProvider))
                {
                    if (targetState == FocusStateType.DeepWork)
                    {
                        state.GoToAway();
                        state.TryEnterDeepWork();
                    }
                    else
                    {
                        state.GoToAway();
                    }

                    activity?.SetTag("matched_signal", "blackout");
                    activity?.SetTag("focus_state", targetState.ToString()!);
                    Log.StateResolved(_logger, userId, targetState.ToString()!, $"blackout: {dto.Label}");
                    return state;
                }
            }
            catch (Exception ex)
            {
                Log.BlackoutEvaluationFailed(_logger, dto.Label, ex);
            }
        }

        // Signal 3: Outside working hours → Away
        var localTime = TimeOnly.FromDateTime(utcNow.DateTime);
        if (localTime < opts.WorkingHoursStart || localTime >= opts.WorkingHoursEnd)
        {
            state.GoToAway();
            activity?.SetTag("matched_signal", "outside_hours");
            activity?.SetTag("focus_state", "Away");
            Log.StateResolved(_logger, userId, "Away", "outside_hours");
            return state;
        }

        // Signal 4: Fallback → WindowOfOpportunity
        activity?.SetTag("matched_signal", "fallback");
        activity?.SetTag("focus_state", "WindowOfOpportunity");
        Log.StateResolved(_logger, userId, "WindowOfOpportunity", "fallback");
        return state;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4801, Level = LogLevel.Information,
            Message = "Focus state resolved for user {UserId}: {State} via {Signal}")]
        public static partial void StateResolved(ILogger logger, string userId, string state, string signal);

        [LoggerMessage(EventId = 4802, Level = LogLevel.Warning,
            Message = "Calendar store query failed during focus state resolution")]
        public static partial void CalendarStoreFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4803, Level = LogLevel.Warning,
            Message = "Blackout period '{Label}' evaluation failed")]
        public static partial void BlackoutEvaluationFailed(ILogger logger, string label, Exception exception);

        [LoggerMessage(EventId = 4804, Level = LogLevel.Warning,
            Message = "Calendar store returned events but no match found for user {UserId}")]
        public static partial void NoCalendarStoreMatch(ILogger logger, string userId);
    }
}
