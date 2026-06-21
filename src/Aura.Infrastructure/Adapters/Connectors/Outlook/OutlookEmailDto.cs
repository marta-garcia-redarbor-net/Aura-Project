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
}
