using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Connectors.Outlook;

internal sealed class OutlookWorkItemMapper
{
    private static readonly string[] SubjectPriorityCues =
    [
        "urgent",
        "escalation",
        "incident",
        "asap"
    ];

    private static readonly string[] BodyHighPriorityCues =
    [
        "production down",
        "sev1",
        "immediate"
    ];

    private static readonly string[] BodyMediumPriorityCues =
    [
        "follow up",
        "review today",
        "needs attention"
    ];

    private static readonly string[] HighTierSenderCues =
    [
        "ceo@",
        "cto@",
        "vp@"
    ];

    private static readonly string[] MediumTierSenderCues =
    [
        "director@",
        "manager@"
    ];

    private static readonly string[] DeadlineCues =
    [
        "due",
        "by eod",
        "by end of day",
        "deadline",
        "until"
    ];

    public bool TryMap(OutlookEmailDto email, out WorkItem? workItem)
    {
        ArgumentNullException.ThrowIfNull(email);

        workItem = null;
        if (string.IsNullOrWhiteSpace(email.ExternalId))
        {
            return false;
        }

        var metadata = BuildMetadata(email);

        var title = ResolveTitle(email, metadata);
        var priority = ResolvePriority(email.Importance, email.Subject, email.SenderAddress, email.BodyPreview, metadata);
        ScanDeadlineCues(email.Subject, email.BodyPreview, metadata);

        workItem = new WorkItem(
            externalId: email.ExternalId,
            title: title,
            source: "inbox",
            sourceType: WorkItemSourceType.OutlookEmail,
            priority: priority,
            metadata: metadata,
            correlationId: email.CorrelationId,
            capturedAtUtc: email.ReceivedDateTime,
            ownerUserId: email.UserOid);

        return true;
    }

    private static string ResolveTitle(OutlookEmailDto email, IDictionary<string, string> metadata)
    {
        if (!string.IsNullOrWhiteSpace(email.Subject))
        {
            return email.Subject;
        }

        metadata[WorkItemSignalKeys.OutlookSubjectRaw] = "absent";
        metadata[WorkItemSignalKeys.OutlookSubjectResolution] = "defaulted";
        return $"Outlook email {email.ExternalId}";
    }

    private static Dictionary<string, string> BuildMetadata(OutlookEmailDto email)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(email.SenderAddress))
        {
            metadata[WorkItemSignalKeys.OutlookSender] = email.SenderAddress;
            metadata[WorkItemSignalKeys.CanonicalSender] = email.SenderAddress;
        }

        if (!string.IsNullOrWhiteSpace(email.UserOid))
        {
            metadata[WorkItemSignalKeys.TargetOwnerUserId] = email.UserOid;
        }

        if (!string.IsNullOrWhiteSpace(email.ConversationId))
        {
            metadata[WorkItemSignalKeys.OutlookConversationId] = email.ConversationId;
        }

        // New Graph-sourced fields
        if (!string.IsNullOrWhiteSpace(email.WebLink))
        {
            metadata[WorkItemSignalKeys.OutlookDeepLink] = email.WebLink;
        }

        if (!string.IsNullOrWhiteSpace(email.BodyPreview))
        {
            metadata[WorkItemSignalKeys.OutlookSnippet] = email.BodyPreview;
            metadata[WorkItemSignalKeys.CanonicalSnippet] = email.BodyPreview;
        }

        return metadata;
    }

    private static WorkItemPriority ResolvePriority(
        string? importance,
        string? subject,
        string? sender,
        string? body,
        IDictionary<string, string> metadata)
    {
        var importanceWeight = ScoreImportance(importance);
        var (subjectWeight, subjectTokens) = ScoreSubject(subject);
        var senderWeight = ScoreSender(sender);
        var (bodyWeight, bodyTokens) = ScoreBody(body);

        var totalScore = importanceWeight + subjectWeight + senderWeight + bodyWeight;

        metadata[WorkItemSignalKeys.OutlookImportanceRaw] = string.IsNullOrWhiteSpace(importance) ? "absent" : importance;
        if (!string.IsNullOrWhiteSpace(importance))
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = importance.Equals("High", StringComparison.OrdinalIgnoreCase)
                ? SignalLevel.Critical.ToString()
                : SignalLevel.Medium.ToString();
        }
        metadata[WorkItemSignalKeys.OutlookScoringSubjectCues] = subjectTokens.Count == 0 ? "none" : string.Join(',', subjectTokens);
        metadata[WorkItemSignalKeys.OutlookScoringSenderWeight] = senderWeight.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metadata[WorkItemSignalKeys.OutlookScoringBodyCues] = bodyTokens.Count == 0 ? "none" : string.Join(',', bodyTokens);
        metadata[WorkItemSignalKeys.OutlookScoringTotalScore] = totalScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
        metadata[WorkItemSignalKeys.ActionNeededSignal] = (subjectTokens.Count > 0 || bodyTokens.Count > 0).ToString();
        metadata[WorkItemSignalKeys.MessageLengthBucketSignal] = string.IsNullOrWhiteSpace(body)
            ? "short"
            : body.Length > 160 ? "long" : "short";

        if (totalScore >= 6) return WorkItemPriority.Critical;
        if (totalScore >= 2) return WorkItemPriority.High;
        if (totalScore >= 0) return WorkItemPriority.Medium;
        return WorkItemPriority.Low;
    }

    private static int ScoreImportance(string? importance)
    {
        if (string.IsNullOrWhiteSpace(importance)) return 0;
        if (importance.Equals("high", StringComparison.OrdinalIgnoreCase)) return 3;
        if (importance.Equals("normal", StringComparison.OrdinalIgnoreCase)) return 1;
        if (importance.Equals("low", StringComparison.OrdinalIgnoreCase)) return -1;
        return 0;
    }

    private static (int Weight, IReadOnlyList<string> Tokens) ScoreSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return (0, []);

        var matches = MatchTokens(subject, SubjectPriorityCues);
        return (matches.Count > 0 ? 1 : 0, matches);
    }

    private static int ScoreSender(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender)) return 0;

        if (HighTierSenderCues.Any(cue => sender.Contains(cue, StringComparison.OrdinalIgnoreCase))) return 2;
        if (MediumTierSenderCues.Any(cue => sender.Contains(cue, StringComparison.OrdinalIgnoreCase))) return 1;
        return 0;
    }

    private static (int Weight, IReadOnlyList<string> Tokens) ScoreBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return (0, []);

        var highMatches = MatchTokens(body, BodyHighPriorityCues);
        if (highMatches.Count > 0)
        {
            return (2, highMatches);
        }

        var mediumMatches = MatchTokens(body, BodyMediumPriorityCues);
        if (mediumMatches.Count > 0)
        {
            return (1, mediumMatches);
        }

        return (0, []);
    }

    private static void ScanDeadlineCues(string? subject, string? body, IDictionary<string, string> metadata)
    {
        if (TryFindDeadlineCue(subject, out var subjectCue))
        {
            metadata[WorkItemSignalKeys.OutlookDeadlineCue] = subjectCue;
            metadata[WorkItemSignalKeys.OutlookDeadlineSource] = "subject";
            return;
        }

        if (TryFindDeadlineCue(body, out var bodyCue))
        {
            metadata[WorkItemSignalKeys.OutlookDeadlineCue] = bodyCue;
            metadata[WorkItemSignalKeys.OutlookDeadlineSource] = "body";
        }
    }

    private static bool TryFindDeadlineCue(string? value, out string cue)
    {
        cue = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        foreach (var token in DeadlineCues)
        {
            var tokenIndex = value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (tokenIndex >= 0)
            {
                cue = ExtractCueContext(value, tokenIndex, token.Length);
                return true;
            }
        }

        var datePattern = new System.Text.RegularExpressions.Regex("\\b\\d{1,2}[/-]\\d{1,2}\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match = datePattern.Match(value);
        if (match.Success)
        {
            cue = ExtractCueContext(value, match.Index, match.Length);
            return true;
        }

        return false;
    }

    private static List<string> MatchTokens(string source, IEnumerable<string> tokens)
    {
        var matches = new List<string>();
        foreach (var token in tokens)
        {
            if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(token);
            }
        }

        return matches;
    }

    private static string ExtractCueContext(string source, int matchIndex, int matchLength)
    {
        const int maxLength = 100;
        var contextBefore = Math.Min(20, matchIndex);
        var start = matchIndex - contextBefore;
        var minLength = contextBefore + matchLength;
        var availableLength = source.Length - start;
        var length = Math.Min(maxLength, Math.Max(minLength, availableLength));
        return source.Substring(start, length).Trim();
    }
}
