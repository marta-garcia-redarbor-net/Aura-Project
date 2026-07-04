using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Connectors.PrReview;

internal sealed class PrReviewWorkItemMapper
{
    public bool TryMap(PrReviewDto pr, out WorkItem? workItem)
    {
        ArgumentNullException.ThrowIfNull(pr);

        workItem = null;

        if (pr.PullRequestId <= 0)
        {
            return false;
        }

        var externalId = $"pr-{pr.PullRequestId}";
        var title = string.IsNullOrWhiteSpace(pr.Title)
            ? $"PR #{pr.PullRequestId}"
            : pr.Title;

        var metadata = BuildMetadata(pr);
        var priority = ResolvePriority(pr, metadata);

        workItem = new WorkItem(
            externalId: externalId,
            title: title,
            source: "pr",
            sourceType: WorkItemSourceType.PrReview,
            priority: priority,
            metadata: metadata,
            capturedAtUtc: pr.CreatedAt);

        return true;
    }

    private static Dictionary<string, string> BuildMetadata(PrReviewDto pr)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        metadata["pr.pullRequestId"] = pr.PullRequestId.ToString();

        if (!string.IsNullOrWhiteSpace(pr.Status))
            metadata["pr.status"] = pr.Status;

        if (!string.IsNullOrWhiteSpace(pr.RepoName))
            metadata["pr.repo"] = pr.RepoName;

        if (!string.IsNullOrWhiteSpace(pr.Author))
        {
            metadata["pr.author"] = pr.Author;
            metadata[WorkItemSignalKeys.CanonicalSender] = pr.Author;
            metadata[WorkItemSignalKeys.TargetOwnerUserId] = pr.Author;
        }

        if (!string.IsNullOrWhiteSpace(pr.SourceLink))
            metadata["pr.sourceLink"] = pr.SourceLink;

        if (pr.UpdatedAt.HasValue)
            metadata["pr.updatedAt"] = pr.UpdatedAt.Value.ToString("o");

        if (pr.Reviewers?.Count > 0)
        {
            metadata["pr.reviewers"] = string.Join(",", pr.Reviewers);
            metadata[WorkItemSignalKeys.TargetResponsibleUserId] = pr.Reviewers[0];
        }

        metadata["pr.reviewerCount"] = (pr.Reviewers?.Count ?? 0).ToString();
        metadata["pr.commentCount"] = pr.CommentCount.ToString();
        metadata["pr.fileCount"] = pr.FileCount.ToString();
        metadata["pr.isDraft"] = pr.IsDraft.ToString();
        metadata[WorkItemSignalKeys.ActionNeededSignal] = ((pr.Reviewers?.Count ?? 0) > 0).ToString();
        metadata[WorkItemSignalKeys.CanonicalSnippet] = pr.Title ?? string.Empty;

        return metadata;
    }

    private static WorkItemPriority ResolvePriority(PrReviewDto pr, IDictionary<string, string> metadata)
    {
        if (!string.IsNullOrWhiteSpace(pr.Priority))
        {
            if (pr.Priority.Equals("critical", StringComparison.OrdinalIgnoreCase))
                return WorkItemPriority.Critical;
            if (pr.Priority.Equals("high", StringComparison.OrdinalIgnoreCase))
                return WorkItemPriority.High;
            if (pr.Priority.Equals("medium", StringComparison.OrdinalIgnoreCase))
                return WorkItemPriority.Medium;
            if (pr.Priority.Equals("low", StringComparison.OrdinalIgnoreCase))
                return WorkItemPriority.Low;

            metadata["pr.priority.raw"] = pr.Priority;
            metadata["pr.priority.resolution"] = "defaulted-medium";
            return WorkItemPriority.Medium;
        }

        // Default draft PRs to Low, active PRs to Medium
        if (pr.IsDraft)
        {
            metadata["pr.priority.raw"] = "absent";
            metadata["pr.priority.resolution"] = "defaulted-low-draft";
            return WorkItemPriority.Low;
        }

        metadata["pr.priority.raw"] = "absent";
        metadata["pr.priority.resolution"] = "defaulted-medium";
        return WorkItemPriority.Medium;
    }
}
