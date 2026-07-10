namespace Aura.Application.Models;

public static class PrMetadataKeys
{
    public const string Repo = "pr.repo";
    public const string Author = "pr.author";
    public const string Status = "pr.status";
    public const string ReviewerCount = "pr.reviewerCount";
    public const string CommentCount = "pr.commentCount";
    public const string FileCount = "pr.fileCount";
    public const string SourceLink = "pr.sourceLink";
    public const string IsDraft = "pr.isDraft";
    public const string UpdatedAt = "pr.updatedAt";

    public const string AttentionScope = "pr.attentionScope";
    public const string AttentionScopeFallback = "pr.attentionScope.fallback";

    public static string ReviewerOid(int index) => $"pr.reviewer.{index}.oid";

    public static string ReviewerDisplayName(int index) => $"pr.reviewer.{index}.displayName";

    public static string ReviewerIsContainer(int index) => $"pr.reviewer.{index}.isContainer";
}
