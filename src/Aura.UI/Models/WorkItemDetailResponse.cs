namespace Aura.UI.Models;

public sealed record WorkItemDetailResponse(
    Guid Id,
    string ExternalId,
    string Title,
    string Source,
    string SourceType,
    string Status,
    string Priority,
    string RelativeTimestamp,
    DateTimeOffset CapturedAtUtc)
{
    public string? Sender { get; init; }
    public string? Channel { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? SuggestedAction { get; init; }
}
