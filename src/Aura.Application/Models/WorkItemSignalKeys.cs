namespace Aura.Application.Models;

/// <summary>
/// Canonical metadata keys used by Morning Summary ranking policy.
/// </summary>
public static class WorkItemSignalKeys
{
    // ── Canonical / normalized signals ──────────────────────────────
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

    /// <summary>
    /// Marker for work items injected by the demo/simulation mode.
    /// Value is a boolean string ("true"/"false"). Added by DemoService when creating seed data.
    /// </summary>
    public const string IsDemo = "is_demo";

    /// <summary>
    /// Current normalized risk score key used by deterministic ranking policy.
    /// </summary>
    public const string RiskScore = "triage.risk.score";

    // ── Outlook raw metadata ───────────────────────────────────────
    public const string OutlookSubjectRaw = "outlook.subject.raw";
    public const string OutlookSubjectResolution = "outlook.subject.resolution";
    public const string OutlookSender = "outlook.sender";
    public const string OutlookConversationId = "outlook.conversationId";
    public const string OutlookDeepLink = "outlook.deepLink";
    public const string OutlookSnippet = "outlook.snippet";
    public const string OutlookImportanceRaw = "outlook.importance.raw";
    public const string OutlookScoringSubjectCues = "outlook.scoring.subjectCues";
    public const string OutlookScoringSenderWeight = "outlook.scoring.senderWeight";
    public const string OutlookScoringBodyCues = "outlook.scoring.bodyCues";
    public const string OutlookScoringTotalScore = "outlook.scoring.totalScore";
    public const string OutlookDeadlineCue = "outlook.deadline.cue";
    public const string OutlookDeadlineSource = "outlook.deadline.source";
    public const string OutlookDeadlineAtUtc = "outlook.deadline.atUtc";

    // ── Teams raw metadata ─────────────────────────────────────────
    public const string TeamsTitleRaw = "teams.title.raw";
    public const string TeamsTitleResolution = "teams.title.resolution";
    public const string TeamsSourceRaw = "teams.source.raw";
    public const string TeamsSourceResolution = "teams.source.resolution";
    public const string TeamsTeamId = "teams.teamId";
    public const string TeamsChannelId = "teams.channelId";
    public const string TeamsMessageUrl = "teams.messageUrl";
    public const string TeamsSender = "teams.sender";
    public const string TeamsSnippet = "teams.snippet";
    public const string TeamsDeepLink = "teams.deepLink";
    public const string TeamsPriorityRaw = "teams.priority.raw";
    public const string TeamsPriorityResolution = "teams.priority.resolution";
    public const string TeamsScoringTitleCues = "teams.scoring.titleCues";
    public const string TeamsScoringBodyCues = "teams.scoring.bodyCues";
    public const string TeamsScoringMentionDetected = "teams.scoring.mentionDetected";
    public const string TeamsScoringTotalScore = "teams.scoring.totalScore";
    public const string TeamsDeadlineCue = "teams.deadline.cue";
    public const string TeamsDeadlineSource = "teams.deadline.source";

    // ── Teams chat raw metadata ────────────────────────────────────
    public const string ChatsLastMessageAt = "chats.lastMessageAt";
    public const string ChatsLastMessageReadAt = "chats.lastMessageReadAt";
    public const string ChatsUnreadCount = "chats.unreadCount";
}
