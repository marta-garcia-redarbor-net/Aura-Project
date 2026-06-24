using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Application.UseCases.MorningSummary;
using Aura.Domain.WorkItems;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class DashboardPreviewReaderTests
{
    [Fact]
    public async Task GetAsync_WithRankedItems_ProjectsInboxGroupsAndSummaryEntries()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var workItemReader = Substitute.For<IWorkItemReader>();
        workItemReader.ReadForWindowAsync(Arg.Any<MorningSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItem>>([
                CreateWorkItem("a", "outlook", WorkItemSourceType.OutlookEmail, WorkItemPriority.High,
                    new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero)),
                CreateWorkItem("b", "teams", WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
                    new DateTimeOffset(2026, 6, 23, 9, 30, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.Equal(2, result.InboxGroups.Count);
        Assert.Equal(2, result.SummaryEntries.Count);

        var outlookGroup = Assert.Single(result.InboxGroups.Where(group => group.Source == "outlook"));
        var outlookItem = Assert.Single(outlookGroup.Items);
        Assert.Equal("Item a", outlookItem.Title);
        Assert.Equal("outlook", outlookItem.Source);
        Assert.Equal("2h ago", outlookItem.RelativeTimestamp);
        Assert.Equal("Review and reply", outlookItem.SuggestedAction);

        var teamsGroup = Assert.Single(result.InboxGroups.Where(group => group.Source == "teams"));
        var teamsItem = Assert.Single(teamsGroup.Items);
        Assert.Equal("30m ago", teamsItem.RelativeTimestamp);
        Assert.Equal("Review and respond", teamsItem.SuggestedAction);

        Assert.Equal("Item b", result.SummaryEntries[0].Title);
        Assert.Equal("teams", result.SummaryEntries[0].Source);
        Assert.Equal(1, result.SummaryEntries[0].Rank);
    }

    [Fact]
    public async Task GetAsync_WithoutWorkItemReaderRegistration_ReturnsEmptyPreview()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var reader = new DashboardPreviewReader(
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.Empty(result.InboxGroups);
        Assert.Empty(result.SummaryEntries);
    }

    [Fact]
    public async Task GetAsync_WithMetadata_PopulatesSenderSnippetDeepLinkPriorityHintSyncState()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var teamsMetadata = new Dictionary<string, string>
        {
            ["teams.sender"] = "Alice",
            ["teams.snippet"] = "Please review PR #42",
            ["teams.deepLink"] = "https://teams.microsoft.com/msg/42"
        };

        var outlookMetadata = new Dictionary<string, string>
        {
            ["outlook.sender"] = "ceo@aura.dev",
            ["outlook.snippet"] = "production down immediate action required",
            ["outlook.deepLink"] = "https://outlook.office.com/mail/id/AAA"
        };

        var workItemReader = Substitute.For<IWorkItemReader>();
        workItemReader.ReadForWindowAsync(Arg.Any<MorningSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItem>>([
                new WorkItem("ext-teams-1", "PR Review needed", "teams",
                    WorkItemSourceType.TeamsMessage, WorkItemPriority.High, teamsMetadata,
                    "corr-1", new DateTimeOffset(2026, 6, 23, 9, 30, 0, TimeSpan.Zero)),
                new WorkItem("ext-outlook-1", "Urgent incident escalation", "outlook",
                    WorkItemSourceType.OutlookEmail, WorkItemPriority.Critical, outlookMetadata,
                    "corr-2", new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var teamsGroup = Assert.Single(result.InboxGroups.Where(g => g.Source == "teams"));
        var teamsItem = Assert.Single(teamsGroup.Items);
        Assert.Equal("Alice", teamsItem.Sender);
        Assert.Equal("Please review PR #42", teamsItem.Snippet);
        Assert.Equal("https://teams.microsoft.com/msg/42", teamsItem.DeepLink);
        Assert.Equal("High", teamsItem.PriorityHint);
        Assert.Equal("synced", teamsItem.SyncState);

        var outlookGroup = Assert.Single(result.InboxGroups.Where(g => g.Source == "outlook"));
        var outlookItem = Assert.Single(outlookGroup.Items);
        Assert.Equal("ceo@aura.dev", outlookItem.Sender);
        Assert.Equal("production down immediate action required", outlookItem.Snippet);
        Assert.Equal("https://outlook.office.com/mail/id/AAA", outlookItem.DeepLink);
        Assert.Equal("Critical", outlookItem.PriorityHint);
        Assert.Equal("synced", outlookItem.SyncState);
    }

    [Fact]
    public async Task GetAsync_WithEmptyMetadata_LeavesNewFieldsNull()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var workItemReader = Substitute.For<IWorkItemReader>();
        workItemReader.ReadForWindowAsync(Arg.Any<MorningSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItem>>([
                new WorkItem("ext-1", "No metadata item", "teams",
                    WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
                    new Dictionary<string, string>(),
                    "corr-1", new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var teamsGroup = Assert.Single(result.InboxGroups);
        var item = Assert.Single(teamsGroup.Items);
        Assert.Null(item.Sender);
        Assert.Null(item.Snippet);
        Assert.Null(item.DeepLink);
        Assert.Equal("Medium", item.PriorityHint);
        Assert.Null(item.SyncState);
    }

    private static WorkItem CreateWorkItem(
        string externalId,
        string source,
        WorkItemSourceType sourceType,
        WorkItemPriority priority,
        DateTimeOffset capturedAtUtc)
    {
        return new WorkItem(
            externalId,
            $"Item {externalId}",
            source,
            sourceType,
            priority,
            new Dictionary<string, string>(),
            correlationId: $"corr-{externalId}",
            capturedAtUtc: capturedAtUtc);
    }
}
