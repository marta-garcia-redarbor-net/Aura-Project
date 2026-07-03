namespace Aura.Infrastructure.Adapters.Options;

/// <summary>
/// Configuration options for the interruption policy engine and its rules.
/// </summary>
public sealed class InterruptionOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "InterruptionOptions";

    /// <summary>
    /// Minimum score threshold for the <see cref="Services.Rules.ScoreThresholdRule"/> (default: 6).
    /// </summary>
    public double UrgentThreshold { get; set; } = 6.0;

    /// <summary>
    /// Hours before a deadline within which the <see cref="Services.Rules.DeadlineUrgencyRule"/> triggers (default: 2).
    /// </summary>
    public double DeadlineWindowHours { get; set; } = 2.0;
}
