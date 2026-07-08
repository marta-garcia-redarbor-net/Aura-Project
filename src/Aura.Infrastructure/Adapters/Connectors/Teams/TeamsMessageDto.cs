namespace Aura.Infrastructure.Adapters.Connectors.Teams;

internal sealed record TeamsMessageDto
{
    public string? ExternalId { get; init; }

    public string? Title { get; init; }

    public string? Source { get; init; }

    public string? Priority { get; init; }

    public string? TeamId { get; init; }

    public string? ChannelId { get; init; }

    public string? MessageUrl { get; init; }

    public string? CorrelationId { get; init; }

    /// <summary>Canonical Entra oid of the user whose Teams data is being synchronized.</summary>
    public string? UserOid { get; init; }

    public DateTimeOffset? CapturedAtUtc { get; init; }

    /// <summary>Display name of the message sender (from Graph API).</summary>
    public string? Sender { get; init; }

    /// <summary>Short body preview of the message.</summary>
    public string? BodyPreview { get; init; }

    /// <summary>Deep link URL to the message in Teams web client.</summary>
    public string? WebUrl { get; init; }

    /// <summary>Timestamp when the chat was last read by the user (from Graph API).</summary>
    public DateTimeOffset? LastMessageReadAt { get; init; }

    /// <summary>Timestamp of the last message in the chat.</summary>
    public DateTimeOffset? LastMessageAt { get; init; }

    /// <summary>Number of unread messages in the chat.</summary>
    public int UnreadCount { get; init; }
}
