using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.MorningSummaryScheduling;

internal sealed class AppSettingsMorningSummarySettingsProvider : IMorningSummarySettingsProvider
{
    private static readonly TimeOnly DefaultTargetLocalTime = new(9, 0);
    private readonly IOptionsMonitor<MorningSummaryOptions> _options;

    public AppSettingsMorningSummarySettingsProvider(IOptionsMonitor<MorningSummaryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public MorningSummarySettings GetSettings()
    {
        var options = _options.CurrentValue;
        var targetLocalTime = ParseTargetLocalTime(options.TargetLocalTime);

        return new MorningSummarySettings(
            TimezoneId: options.TimezoneId,
            TargetLocalTime: targetLocalTime);
    }

    private static TimeOnly ParseTargetLocalTime(string? value)
    {
        if (TimeOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return DefaultTargetLocalTime;
    }
}
