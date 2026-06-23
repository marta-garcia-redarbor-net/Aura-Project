namespace Aura.Infrastructure.Adapters.MorningSummaryScheduling;

internal sealed class MorningSummaryOptions
{
    internal const string SectionName = "MorningSummary";

    public string? TimezoneId { get; set; }

    public string TargetLocalTime { get; set; } = "09:00";
}
