namespace Aura.UI.Models;

public sealed record PrPreviewItemResponse(
    string Title,
    string PrDisplayName,
    string BranchName,
    string BuildStatus,
    int ReviewApprovals,
    int ReviewRequired,
    int ReviewChangesRequested,
    string Author,
    DateTimeOffset UpdatedAt,
    string RelativeTimestamp,
    string SourceLink,
    bool IsDraft,
    string Priority);
