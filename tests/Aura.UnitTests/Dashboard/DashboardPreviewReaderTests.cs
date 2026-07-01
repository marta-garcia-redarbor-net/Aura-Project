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
        Assert.Equal("Critical", result.SummaryEntries[0].PriorityHint);
        Assert.Equal("High", result.SummaryEntries[1].PriorityHint);
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

    [Fact]
    public async Task GetAsync_WithMessagesSource_ResolvesTeamsMetadataPrefix()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var metadata = new Dictionary<string, string>
        {
            ["teams.sender"] = "Bob",
            ["teams.snippet"] = "Deploy blocked on auth service",
            ["teams.deepLink"] = "https://teams.microsoft.com/msg/99"
        };

        var workItemReader = Substitute.For<IWorkItemReader>();
        workItemReader.ReadForWindowAsync(Arg.Any<MorningSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItem>>([
                new WorkItem("msg-1", "Auth service issue", "messages",
                    WorkItemSourceType.TeamsMessage, WorkItemPriority.High, metadata,
                    "corr-1", new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var group = Assert.Single(result.InboxGroups);
        var item = Assert.Single(group.Items);
        Assert.Equal("messages", item.Source);
        Assert.Equal("Bob", item.Sender);
        Assert.Equal("Deploy blocked on auth service", item.Snippet);
        Assert.Equal("https://teams.microsoft.com/msg/99", item.DeepLink);
        Assert.Equal("synced", item.SyncState);
    }

    [Fact]
    public async Task GetAsync_WithInboxSource_ResolvesOutlookMetadataPrefix()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUser().Returns(new AuraUser
        {
            UserId = "user-42",
            DisplayName = "Preview User",
            Email = "preview@aura.test"
        });

        var metadata = new Dictionary<string, string>
        {
            ["outlook.sender"] = "cto@aura.dev",
            ["outlook.snippet"] = "Incident P1: database replication lag",
            ["outlook.deepLink"] = "https://outlook.office.com/mail/id/777"
        };

        var workItemReader = Substitute.For<IWorkItemReader>();
        workItemReader.ReadForWindowAsync(Arg.Any<MorningSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItem>>([
                new WorkItem("mail-1", "Incident P1", "inbox",
                    WorkItemSourceType.OutlookEmail, WorkItemPriority.Critical, metadata,
                    "corr-2", new DateTimeOffset(2026, 6, 23, 8, 30, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var group = Assert.Single(result.InboxGroups);
        var item = Assert.Single(group.Items);
        Assert.Equal("inbox", item.Source);
        Assert.Equal("cto@aura.dev", item.Sender);
        Assert.Equal("Incident P1: database replication lag", item.Snippet);
        Assert.Equal("https://outlook.office.com/mail/id/777", item.DeepLink);
        Assert.Equal("synced", item.SyncState);
    }

    [Fact]
    public async Task GetAsync_WithMessagesSource_ReturnsReviewAndRespondAction()
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
                CreateWorkItem("msg-1", "messages", WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
                    new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var group = Assert.Single(result.InboxGroups);
        var item = Assert.Single(group.Items);
        Assert.Equal("Review and respond", item.SuggestedAction);
    }

    [Fact]
    public async Task GetAsync_WithInboxSource_ReturnsReviewAndReplyAction()
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
                CreateWorkItem("mail-1", "inbox", WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium,
                    new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero))
            ]));

        var reader = new DashboardPreviewReader(
            workItemReader,
            new MorningSummaryRankingPolicy(),
            currentUser,
            utcNow: () => new DateTimeOffset(2026, 6, 23, 10, 0, 0, TimeSpan.Zero));

        var result = await reader.GetAsync(CancellationToken.None);

        var group = Assert.Single(result.InboxGroups);
        var item = Assert.Single(group.Items);
        Assert.Equal("Review and reply", item.SuggestedAction);
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
