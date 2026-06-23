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
