namespace Aura.Infrastructure.Adapters.Connectors.Outlook;

internal sealed record OutlookEmailDto
{
    public string? ExternalId { get; init; }

    public string? Subject { get; init; }

    public string? Importance { get; init; }

    public string? SenderAddress { get; init; }

    public string? BodyPreview { get; init; }

    public DateTimeOffset? ReceivedDateTime { get; init; }

    public string? CorrelationId { get; init; }

    public string? ConversationId { get; init; }

    /// <summary>Canonical Entra oid of the user whose mailbox is being synchronized.</summary>
    public string? UserOid { get; init; }

    /// <summary>Deep link URL to open the email in Outlook web.</summary>
    public string? WebLink { get; init; }

    /// <summary>Indicates whether the email has been read in Outlook.</summary>
    public bool IsRead { get; init; }
}
