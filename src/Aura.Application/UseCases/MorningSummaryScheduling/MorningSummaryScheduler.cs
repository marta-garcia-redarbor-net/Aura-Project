using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.UseCases.MorningSummaryScheduling;

public sealed class MorningSummaryScheduler : IMorningSummaryScheduler
{
    private readonly IMorningSummarySettingsProvider _settingsProvider;
    private readonly IMorningSummaryEmissionStore _emissionStore;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<TimeZoneInfo> _systemTimeZoneResolver;

    public MorningSummaryScheduler(
        IMorningSummarySettingsProvider settingsProvider,
        IMorningSummaryEmissionStore emissionStore,
        Func<DateTimeOffset>? utcNow = null,
        Func<TimeZoneInfo>? systemTimeZoneResolver = null)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(emissionStore);

        _settingsProvider = settingsProvider;
        _emissionStore = emissionStore;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _systemTimeZoneResolver = systemTimeZoneResolver ?? (() => TimeZoneInfo.Local);
    }

    public async Task<MorningSummaryDueState> ResolveAsync(string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        ct.ThrowIfCancellationRequested();

        var settings = _settingsProvider.GetSettings();
        var resolvedTimeZone = ResolveTimeZone(settings.TimezoneId);

        var localNow = TimeZoneInfo.ConvertTime(_utcNow(), resolvedTimeZone);
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);

        var alreadyEmitted = await _emissionStore.HasBeenEmittedAsync(userId, localDate, ct);
        var isDue = localTime >= settings.TargetLocalTime && !alreadyEmitted;

        return new MorningSummaryDueState(
            IsDue: isDue,
            ResolvedTimezoneId: resolvedTimeZone.Id,
            LocalDate: localDate,
            TargetLocalTime: settings.TargetLocalTime);
    }

    private TimeZoneInfo ResolveTimeZone(string? configuredTimezoneId)
    {
        if (!string.IsNullOrWhiteSpace(configuredTimezoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(configuredTimezoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        try
        {
            return _systemTimeZoneResolver();
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
