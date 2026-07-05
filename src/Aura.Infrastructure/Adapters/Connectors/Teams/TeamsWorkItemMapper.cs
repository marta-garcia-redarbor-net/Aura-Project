using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Connectors.Teams;

internal sealed class TeamsWorkItemMapper
{
    private static readonly string[] TitlePriorityCues =
    [
        "urgent",
        "asap",
        "blocker",
        "incident"
    ];

    private static readonly string[] BodyHighPriorityCues =
    [
        "production down",
        "sev1",
        "immediate",
        "broken"
    ];

    private static readonly string[] BodyMediumPriorityCues =
    [
        "follow up",
        "review today",
        "needs attention"
    ];

    private static readonly string[] DeadlineCues =
    [
        "due",
        "by eod",
        "by end of day",
        "deadline",
        "until"
    ];

    public bool TryMap(TeamsMessageDto message, out WorkItem? workItem)
    {
        ArgumentNullException.ThrowIfNull(message);

        workItem = null;

        if (string.IsNullOrWhiteSpace(message.ExternalId))
        {
            return false;
        }

        var title = string.IsNullOrWhiteSpace(message.Title)
            ? $"Teams message {message.ExternalId}"
            : message.Title;

        var source = string.IsNullOrWhiteSpace(message.Source)
            ? "messages"
            : message.Source;

        var metadata = BuildMetadata(message);

        if (string.IsNullOrWhiteSpace(message.Title))
        {
            metadata[WorkItemSignalKeys.TeamsTitleRaw] = "absent";
            metadata[WorkItemSignalKeys.TeamsTitleResolution] = "defaulted";
        }

        if (string.IsNullOrWhiteSpace(message.Source))
        {
            metadata[WorkItemSignalKeys.TeamsSourceRaw] = "absent";
            metadata[WorkItemSignalKeys.TeamsSourceResolution] = "defaulted";
        }

        var sourceType = source == "chats"
            ? WorkItemSourceType.TeamsChat
            : WorkItemSourceType.TeamsMessage;

        if (source == "chats")
        {
            AddChatMetadata(message, metadata);
        }

        ScoreContent(message, metadata);
        ScanDeadlineCues(message.Title, message.BodyPreview, metadata);

        var priority = ResolvePriority(message.Priority, metadata);

        workItem = new WorkItem(
            message.ExternalId,
            title,
            source,
            sourceType,
            priority,
            metadata,
            message.CorrelationId,
            message.CapturedAtUtc);

        return true;
    }

    private static Dictionary<string, string> BuildMetadata(TeamsMessageDto message)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(message.TeamId))
        {
            metadata[WorkItemSignalKeys.TeamsTeamId] = message.TeamId;
        }

        if (!string.IsNullOrWhiteSpace(message.ChannelId))
        {
            metadata[WorkItemSignalKeys.TeamsChannelId] = message.ChannelId;
        }

        if (string.IsNullOrWhiteSpace(message.MessageUrl))
        {
            metadata[WorkItemSignalKeys.TeamsMessageUrl] = "absent";
        }
        else
        {
            metadata[WorkItemSignalKeys.TeamsMessageUrl] = message.MessageUrl;
        }

        // New Graph-sourced fields
        if (!string.IsNullOrWhiteSpace(message.Sender))
        {
            metadata[WorkItemSignalKeys.TeamsSender] = message.Sender;
            metadata[WorkItemSignalKeys.CanonicalSender] = message.Sender;
        }

        if (!string.IsNullOrWhiteSpace(message.BodyPreview))
        {
            metadata[WorkItemSignalKeys.TeamsSnippet] = message.BodyPreview;
            metadata[WorkItemSignalKeys.CanonicalSnippet] = message.BodyPreview;
            metadata[WorkItemSignalKeys.MessageLengthBucketSignal] = message.BodyPreview.Length > 160 ? "long" : "short";
        }

        if (!string.IsNullOrWhiteSpace(message.WebUrl))
        {
            metadata[WorkItemSignalKeys.TeamsDeepLink] = message.WebUrl;
        }

        return metadata;
    }

    private static void AddChatMetadata(TeamsMessageDto message, Dictionary<string, string> metadata)
    {
        // Source is guaranteed to be "chats" — caller only enters when source == "chats"
        if (message.LastMessageAt is not null)
            metadata[WorkItemSignalKeys.ChatsLastMessageAt] = message.LastMessageAt.Value.ToString("o");
        if (message.LastMessageReadAt is not null)
            metadata[WorkItemSignalKeys.ChatsLastMessageReadAt] = message.LastMessageReadAt.Value.ToString("o");
        metadata[WorkItemSignalKeys.ChatsUnreadCount] = message.UnreadCount.ToString();
    }

    private static WorkItemPriority ResolvePriority(string? rawPriority, IDictionary<string, string> metadata)
    {
        if (string.IsNullOrWhiteSpace(rawPriority))
        {
            metadata[WorkItemSignalKeys.TeamsPriorityRaw] = "absent";
            metadata[WorkItemSignalKeys.TeamsPriorityResolution] = "defaulted-medium";
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = SignalLevel.Medium.ToString();
            return WorkItemPriority.Medium;
        }

        if (rawPriority.Equals("critical", StringComparison.OrdinalIgnoreCase))
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = SignalLevel.Critical.ToString();
            return WorkItemPriority.Critical;
        }
        if (rawPriority.Equals("high", StringComparison.OrdinalIgnoreCase))
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = SignalLevel.High.ToString();
            return WorkItemPriority.High;
        }
        if (rawPriority.Equals("medium", StringComparison.OrdinalIgnoreCase))
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = SignalLevel.Medium.ToString();
            return WorkItemPriority.Medium;
        }
        if (rawPriority.Equals("low", StringComparison.OrdinalIgnoreCase))
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = SignalLevel.Low.ToString();
            return WorkItemPriority.Low;
        }

        metadata[WorkItemSignalKeys.TeamsPriorityRaw] = rawPriority;
        metadata[WorkItemSignalKeys.TeamsPriorityResolution] = "defaulted-medium";
        return WorkItemPriority.Medium;
    }

    private static void ScoreContent(TeamsMessageDto message, Dictionary<string, string> metadata)
    {
        var (titleWeight, titleTokens) = ScoreTitle(message.Title);
        var (bodyWeight, bodyTokens) = ScoreBody(message.BodyPreview);
        var mentionWeight = DetectMention(message.BodyPreview);
        var totalScore = titleWeight + bodyWeight + mentionWeight;

        if (!string.IsNullOrWhiteSpace(message.Title))
        {
            metadata[WorkItemSignalKeys.TeamsScoringTitleCues] = titleTokens.Count == 0
                ? "none"
                : string.Join(',', titleTokens);
        }

        if (!string.IsNullOrWhiteSpace(message.BodyPreview))
        {
            metadata[WorkItemSignalKeys.TeamsScoringBodyCues] = bodyTokens.Count == 0
                ? "none"
                : string.Join(',', bodyTokens);
            metadata[WorkItemSignalKeys.TeamsScoringMentionDetected] = mentionWeight > 0 ? "True" : "False";
        }

        metadata[WorkItemSignalKeys.TeamsScoringTotalScore] = totalScore.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        var anyCue = titleTokens.Count > 0 || bodyTokens.Count > 0 || mentionWeight > 0;
        if (anyCue)
        {
            metadata[WorkItemSignalKeys.ActionNeededSignal] = "True";
        }
    }

    private static (int Weight, IReadOnlyList<string> Tokens) ScoreTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return (0, []);

        var matches = MatchTokens(title, TitlePriorityCues);
        if (matches.Count == 0) return (0, []);

        var strongTokens = new[] { "urgent", "asap", "blocker", "incident" };
        var hasStrong = matches.Any(m => strongTokens.Contains(m, StringComparer.OrdinalIgnoreCase));

        if (hasStrong || matches.Count >= 2)
        {
            return (3, matches);
        }

        return (1, matches);
    }

    private static (int Weight, IReadOnlyList<string> Tokens) ScoreBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return (0, []);

        var highMatches = MatchTokens(body, BodyHighPriorityCues);
        if (highMatches.Count > 0)
        {
            return (3, highMatches);
        }

        var mediumMatches = MatchTokens(body, BodyMediumPriorityCues);
        if (mediumMatches.Count > 0)
        {
            return (1, mediumMatches);
        }

        return (0, []);
    }

    private static int DetectMention(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return 0;
        return body.Contains('@') ? 1 : 0;
    }

    private static void ScanDeadlineCues(string? title, string? body, Dictionary<string, string> metadata)
    {
        if (TryFindDeadlineCue(title, out var titleCue))
        {
            metadata[WorkItemSignalKeys.TeamsDeadlineCue] = titleCue;
            metadata[WorkItemSignalKeys.TeamsDeadlineSource] = "title";
            return;
        }

        if (TryFindDeadlineCue(body, out var bodyCue))
        {
            metadata[WorkItemSignalKeys.TeamsDeadlineCue] = bodyCue;
            metadata[WorkItemSignalKeys.TeamsDeadlineSource] = "body";
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
