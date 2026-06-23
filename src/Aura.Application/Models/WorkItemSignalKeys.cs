namespace Aura.Application.Models;

/// <summary>
/// Canonical metadata keys used by Morning Summary ranking policy.
/// </summary>
public static class WorkItemSignalKeys
{
    public const string OutlookDeadlineCue = "outlook.deadline.cue";
    public const string OutlookDeadlineSource = "outlook.deadline.source";
    public const string OutlookTotalScore = "outlook.scoring.totalScore";
    public const string TeamsPriorityRaw = "teams.priority.raw";

    /// <summary>
    /// Current normalized risk score key used by deterministic ranking policy.
    /// </summary>
    public const string RiskScore = "triage.risk.score";

    /// <summary>
    /// Optional due timestamp metadata key used in tie-breaking.
    /// </summary>
    public const string OutlookDeadlineAtUtc = "outlook.deadline.atUtc";
}
