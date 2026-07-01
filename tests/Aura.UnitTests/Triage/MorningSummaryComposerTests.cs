using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.MorningSummary;
using Aura.Domain.WorkItems;

namespace Aura.UnitTests.Triage;

public sealed class MorningSummaryComposerTests
{
    [Fact]
    public async Task ComposeAsync_ReturnsEntriesOrderedByRankingPolicy()
    {
        var window = CreateWindow();
        var request = new MorningSummaryRequest("user-1", window);

        var highScore = CreateWorkItem(
            "a",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookTotalScore] = "0.90"
            });

        var lowScore = CreateWorkItem(
            "b",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookTotalScore] = "0.20"
            });

        var reader = new StubWorkItemReader([lowScore, highScore]);
        var rankingPolicy = new MorningSummaryRankingPolicy();
        var composer = new MorningSummaryComposer(reader, rankingPolicy, () => new DateTimeOffset(2026, 6, 22, 7, 1, 0, TimeSpan.Zero));

        var summary = await composer.ComposeAsync(request, CancellationToken.None);

        Assert.Equal("a", summary.Entries[0].Item.ExternalId);
        Assert.Equal("b", summary.Entries[1].Item.ExternalId);
    }

    [Fact]
    public async Task ComposeAsync_AlignsPerItemExplanationWithRankingOutput()
    {
        var window = CreateWindow();
        var request = new MorningSummaryRequest("user-1", window);

        var withDeadline = CreateWorkItem(
            "deadline-first",
            WorkItemPriority.Low,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookDeadlineCue] = "due by noon",
                [WorkItemSignalKeys.OutlookDeadlineSource] = "subject"
            });

        var withoutDeadline = CreateWorkItem("no-deadline", WorkItemPriority.Critical);

        var reader = new StubWorkItemReader([withoutDeadline, withDeadline]);
        var rankingPolicy = new MorningSummaryRankingPolicy();
        var composer = new MorningSummaryComposer(reader, rankingPolicy, () => new DateTimeOffset(2026, 6, 22, 7, 1, 0, TimeSpan.Zero));

        var summary = await composer.ComposeAsync(request, CancellationToken.None);

        var top = summary.Entries[0];
        Assert.Equal("deadline-first", top.Item.ExternalId);
        Assert.Equal(RankingFactor.Deadline, top.Explanation.Contributions[0].Factor);
    }

    private static MorningSummaryWindow CreateWindow()
    {
        return new MorningSummaryWindow(
            new DateOnly(2026, 6, 22),
            "Europe/Madrid",
            new TimeOnly(9, 0),
            new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero));
    }

    private static WorkItem CreateWorkItem(string externalId, WorkItemPriority priority, IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new WorkItem(
            externalId,
            $"Item {externalId}",
            "outlook",
            WorkItemSourceType.OutlookEmail,
            priority,
            metadata ?? new Dictionary<string, string>(),
            correlationId: $"corr-{externalId}",
            capturedAtUtc: new DateTimeOffset(2026, 6, 21, 8, 0, 0, TimeSpan.Zero));
    }

    private sealed class StubWorkItemReader : IWorkItemReader
    {
        private readonly IReadOnlyList<WorkItem> _items;

        public StubWorkItemReader(IReadOnlyList<WorkItem> items)
        {
            _items = items;
        }

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(_items);
        }

        public Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
            WorkItemSourceType sourceType,
            WorkItemStatus? statusFilter,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_items);
        }
    }
}
