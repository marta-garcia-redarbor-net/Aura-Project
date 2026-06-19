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

    public DateTimeOffset? CapturedAtUtc { get; init; }
}
