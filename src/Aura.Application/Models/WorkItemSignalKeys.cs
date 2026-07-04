namespace Aura.Application.Models;

/// <summary>
/// Canonical metadata keys used by Morning Summary ranking policy.
/// </summary>
public static class WorkItemSignalKeys
{
    public const string CanonicalSender = "triage.sender";
    public const string CanonicalSnippet = "triage.snippet";
    public const string VipSenderSignal = "vip_sender";
    public const string ActionNeededSignal = "action_needed";
    public const string AckOnlySignal = "ack_only";
    public const string TimeCriticalitySignal = "time_criticality";
    public const string MessageLengthBucketSignal = "message_length_bucket";
    public const string ExplicitPatternKey = "triage.override.pattern";
    public const string TargetOwnerUserId = "triage.target.ownerUserId";
    public const string TargetResponsibleUserId = "triage.target.responsibleUserId";
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
