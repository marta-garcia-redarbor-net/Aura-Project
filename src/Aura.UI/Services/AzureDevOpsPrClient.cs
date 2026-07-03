using Aura.UI.Models;
using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

/// <summary>
/// Client for fetching Azure DevOps pull requests.
/// In v1, returns mock data. Future versions will call the Aura.Api endpoint or ADO directly.
/// </summary>
public sealed partial class AzureDevOpsPrClient : IAzureDevOpsPrClient
{
    private readonly ILogger<AzureDevOpsPrClient> _logger;

    public AzureDevOpsPrClient(ILogger<AzureDevOpsPrClient> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<List<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Log.FetchingPrs(_logger);

        var prs = new List<PullRequestResponse>
        {
            new(
                Id: 142,
                Title: "Hotfix: production crash on payment validation",
                RepoName: "Aura",
                Author: "Carlos Ruiz",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 08, 30, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 09, 15, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 2,
                CommentCount: 12,
                FileCount: 3,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/142",
                IsDraft: false,
                Priority: "critical",
                BranchName: "main",
                SourceBranchName: "hotfix/payment-crash",
                BuildStatus: "passing",
                ReviewApprovals: 1,
                ReviewRequired: 2,
                ReviewChangesRequested: 0
            ),
            new(
                Id: 139,
                Title: "Fix: SSO redirect loop on token expiry",
                RepoName: "Aura.Auth",
                Author: "David Martínez",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 07, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 10, 30, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 1,
                CommentCount: 8,
                FileCount: 5,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura.Auth/pullrequest/139",
                IsDraft: false,
                Priority: "critical",
                BranchName: "main",
                SourceBranchName: "fix/sso-token",
                BuildStatus: "passing",
                ReviewApprovals: 1,
                ReviewRequired: 2,
                ReviewChangesRequested: 1
            ),
            new(
                Id: 145,
                Title: "Feature: Add reporting dashboard v2",
                RepoName: "Aura",
                Author: "Laura Sánchez",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 11, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 14, 00, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 3,
                CommentCount: 5,
                FileCount: 12,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/145",
                IsDraft: false,
                Priority: "high",
                BranchName: "develop",
                SourceBranchName: "feature/reporting-v2",
                BuildStatus: "running",
                ReviewApprovals: 1,
                ReviewRequired: 1,
                ReviewChangesRequested: 0
            ),
            new(
                Id: 141,
                Title: "Refactor: Extract payment gateway adapter",
                RepoName: "Aura.Payments",
                Author: "Pedro Gómez",
                CreatedAt: new DateTimeOffset(2026, 06, 30, 16, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 12, 00, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 1,
                CommentCount: 3,
                FileCount: 8,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura.Payments/pullrequest/141",
                IsDraft: false,
                Priority: "high",
                BranchName: "main",
                SourceBranchName: "refactor/payment-adapter",
                BuildStatus: "passing",
                ReviewApprovals: 2,
                ReviewRequired: 2,
                ReviewChangesRequested: 0
            ),
            new(
                Id: 148,
                Title: "Chore: Update dependency versions",
                RepoName: "Aura",
                Author: "Sistema",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 06, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 06, 30, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 0,
                CommentCount: 0,
                FileCount: 15,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/148",
                IsDraft: true,
                Priority: "low",
                BranchName: "develop",
                SourceBranchName: "chore/deps-july",
                BuildStatus: "pending",
                ReviewApprovals: 0,
                ReviewRequired: 1,
                ReviewChangesRequested: 0
            ),
            new(
                Id: 150,
                Title: "Docs: Update API reference for v3 endpoints",
                RepoName: "Aura.Docs",
                Author: "María García",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 09, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 09, 45, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 1,
                CommentCount: 1,
                FileCount: 4,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura.Docs/pullrequest/150",
                IsDraft: false,
                Priority: "low",
                BranchName: "main",
                SourceBranchName: "docs/v3-endpoints",
                BuildStatus: "failed",
                ReviewApprovals: 0,
                ReviewRequired: 1,
                ReviewChangesRequested: 2
            )
        };

        Log.ReturnedPrs(_logger, prs.Count);
        return Task.FromResult(prs);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 6101, Level = LogLevel.Information,
            Message = "AzureDevOpsPrClient: fetching pending PRs (v1 mock)")]
        public static partial void FetchingPrs(ILogger logger);

        [LoggerMessage(EventId = 6102, Level = LogLevel.Information,
            Message = "AzureDevOpsPrClient: returning {Count} PRs")]
        public static partial void ReturnedPrs(ILogger logger, int count);
    }
}
