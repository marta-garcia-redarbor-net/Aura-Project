using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Connectors.Teams;

internal sealed class TeamsWorkItemMapper
{
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
            metadata["teams.title.raw"] = "absent";
            metadata["teams.title.resolution"] = "defaulted";
        }

        if (string.IsNullOrWhiteSpace(message.Source))
        {
            metadata["teams.source.raw"] = "absent";
            metadata["teams.source.resolution"] = "defaulted";
        }

        var priority = ResolvePriority(message.Priority, metadata);

        workItem = new WorkItem(
            message.ExternalId,
            title,
            source,
            WorkItemSourceType.TeamsMessage,
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
            metadata["teams.teamId"] = message.TeamId;
        }

        if (!string.IsNullOrWhiteSpace(message.ChannelId))
        {
            metadata["teams.channelId"] = message.ChannelId;
        }

        if (string.IsNullOrWhiteSpace(message.MessageUrl))
        {
            metadata["teams.messageUrl"] = "absent";
        }
        else
        {
            metadata["teams.messageUrl"] = message.MessageUrl;
        }

        // New Graph-sourced fields
        if (!string.IsNullOrWhiteSpace(message.Sender))
        {
            metadata["teams.sender"] = message.Sender;
        }

        if (!string.IsNullOrWhiteSpace(message.BodyPreview))
        {
            metadata["teams.snippet"] = message.BodyPreview;
        }

        if (!string.IsNullOrWhiteSpace(message.WebUrl))
        {
            metadata["teams.deepLink"] = message.WebUrl;
        }

        return metadata;
    }

    private static WorkItemPriority ResolvePriority(string? rawPriority, IDictionary<string, string> metadata)
    {
        if (string.IsNullOrWhiteSpace(rawPriority))
        {
            metadata["teams.priority.raw"] = "absent";
            metadata["teams.priority.resolution"] = "defaulted-medium";
            return WorkItemPriority.Medium;
        }

        if (rawPriority.Equals("critical", StringComparison.OrdinalIgnoreCase)) return WorkItemPriority.Critical;
        if (rawPriority.Equals("high", StringComparison.OrdinalIgnoreCase)) return WorkItemPriority.High;
        if (rawPriority.Equals("medium", StringComparison.OrdinalIgnoreCase)) return WorkItemPriority.Medium;
        if (rawPriority.Equals("low", StringComparison.OrdinalIgnoreCase)) return WorkItemPriority.Low;

        metadata["teams.priority.raw"] = rawPriority;
        metadata["teams.priority.resolution"] = "defaulted-medium";
        return WorkItemPriority.Medium;
    }
}
