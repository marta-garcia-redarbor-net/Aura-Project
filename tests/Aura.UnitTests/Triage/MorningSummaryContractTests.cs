using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.UnitTests.Triage;

public class MorningSummaryContractTests
{
    [Fact]
    public void MorningSummary_ExposesOrderedNonNullEntries_WithRankItemScoreAndExplanation()
    {
        var window = new MorningSummaryWindow(
            new DateOnly(2026, 6, 22),
            "Europe/Madrid",
            new TimeOnly(9, 0),
            new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero));

        var firstEntry = new RankedWorkItem(
            1,
            CreateWorkItem("external-1", "First"),
            0.95,
            new RankingExplanation(
            [
                new RankingFactorContribution(RankingFactor.Impact, 0.50, "High business impact"),
                new RankingFactorContribution(RankingFactor.Deadline, 0.30, "Due today"),
                new RankingFactorContribution(RankingFactor.Risk, 0.15, "Potential blocker")
            ]));

        var secondEntry = new RankedWorkItem(
            2,
            CreateWorkItem("external-2", "Second"),
            0.70,
            new RankingExplanation(
            [
                new RankingFactorContribution(RankingFactor.Impact, 0.35, "Medium impact"),
                new RankingFactorContribution(RankingFactor.Deadline, 0.20, "Due this week"),
                new RankingFactorContribution(RankingFactor.Risk, 0.15, "Dependencies pending")
            ]));

        var summary = new MorningSummary(
            "user-123",
            window,
            new DateTimeOffset(2026, 6, 22, 7, 1, 0, TimeSpan.Zero),
            [firstEntry, secondEntry]);

        Assert.NotNull(summary.Entries);
        Assert.Collection(
            summary.Entries,
            entry =>
            {
                Assert.Equal(1, entry.Rank);
                Assert.Equal("external-1", entry.Item.ExternalId);
                Assert.Equal(0.95, entry.Score);
                Assert.NotNull(entry.Explanation);
                Assert.Equal(3, entry.Explanation.Contributions.Count);
            },
            entry =>
            {
                Assert.Equal(2, entry.Rank);
                Assert.Equal("external-2", entry.Item.ExternalId);
                Assert.Equal(0.70, entry.Score);
                Assert.NotNull(entry.Explanation);
                Assert.Equal(3, entry.Explanation.Contributions.Count);
            });
    }

    [Fact]
    public void RankingExplanation_ListsFactorContributions_AndRequiredFactorsAreRepresentable()
    {
        var explanation = new RankingExplanation(
        [
            new RankingFactorContribution(RankingFactor.Impact, 0.50, "Impact explanation"),
            new RankingFactorContribution(RankingFactor.Deadline, 0.30, "Deadline explanation"),
            new RankingFactorContribution(RankingFactor.Risk, 0.20, "Risk explanation")
        ]);

        Assert.Collection(
            explanation.Contributions,
            impact =>
            {
                Assert.Equal(RankingFactor.Impact, impact.Factor);
                Assert.Equal(0.50, impact.Value);
                Assert.Equal("Impact explanation", impact.Rationale);
            },
            deadline =>
            {
                Assert.Equal(RankingFactor.Deadline, deadline.Factor);
                Assert.Equal(0.30, deadline.Value);
                Assert.Equal("Deadline explanation", deadline.Rationale);
            },
            risk =>
            {
                Assert.Equal(RankingFactor.Risk, risk.Factor);
                Assert.Equal(0.20, risk.Value);
                Assert.Equal("Risk explanation", risk.Rationale);
            });

        var factors = Enum.GetValues<RankingFactor>();
        Assert.Contains(RankingFactor.Impact, factors);
        Assert.Contains(RankingFactor.Deadline, factors);
        Assert.Contains(RankingFactor.Risk, factors);
    }

    [Fact]
    public void MorningSummary_WithNoEntries_IsValidAndUsesEmptyNonNullCollection()
    {
        var summary = new MorningSummary(
            "user-123",
            new MorningSummaryWindow(
                new DateOnly(2026, 6, 22),
                "Europe/Madrid",
                new TimeOnly(9, 0),
                new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero)),
            new DateTimeOffset(2026, 6, 22, 7, 1, 0, TimeSpan.Zero),
            []);

        Assert.NotNull(summary.Entries);
        Assert.Empty(summary.Entries);
    }

    [Fact]
    public async Task FakeComposer_SatisfiesPort_AndReturnsValidPayload()
    {
        var request = new MorningSummaryRequest(
            "user-123",
            new MorningSummaryWindow(
                new DateOnly(2026, 6, 22),
                "Europe/Madrid",
                new TimeOnly(9, 0),
                new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero)));

        IMorningSummaryComposer composer = new FakeMorningSummaryComposer();

        var result = await composer.ComposeAsync(request, CancellationToken.None);

        Assert.Equal("user-123", result.UserId);
        Assert.Equal(request.Window, result.Window);
        Assert.NotNull(result.Entries);
        Assert.Single(result.Entries);
        Assert.Equal(1, result.Entries[0].Rank);
        Assert.Equal(RankingFactor.Impact, result.Entries[0].Explanation.Contributions[0].Factor);
    }

    [Fact]
    public async Task ComposerContract_CompositionIsDeterministicForCaching_ForSameRequestAndInputs()
    {
        var request = new MorningSummaryRequest(
            "user-123",
            new MorningSummaryWindow(
                new DateOnly(2026, 6, 22),
                "Europe/Madrid",
                new TimeOnly(9, 0),
                new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero)));

        IMorningSummaryComposer composer = new FakeMorningSummaryComposer();

        var first = await composer.ComposeAsync(request, CancellationToken.None);
        var second = await composer.ComposeAsync(request, CancellationToken.None);

        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(first.Window, second.Window);
        Assert.Equal(first.GeneratedAtUtc, second.GeneratedAtUtc);
        Assert.Equal(first.Entries.Count, second.Entries.Count);
        Assert.Equal(first.Entries[0].Rank, second.Entries[0].Rank);
        Assert.Equal(first.Entries[0].Item.ExternalId, second.Entries[0].Item.ExternalId);
        Assert.Equal(first.Entries[0].Score, second.Entries[0].Score);
        Assert.Equal(
            first.Entries[0].Explanation.Contributions[0].Factor,
            second.Entries[0].Explanation.Contributions[0].Factor);
    }

    [Fact]
    public void SchedulerContract_ResolvesAWindow_CarryingDateAndTimezone()
    {
        var context = new MorningSummaryScheduleContext(
            "user-123",
            "Europe/Madrid",
            new TimeOnly(9, 0),
            new DateOnly(2026, 6, 22));

        IMorningSummaryScheduler scheduler = new FakeMorningSummaryScheduler();

        var window = scheduler.ResolveWindow(context);

        Assert.Equal(context.WindowDate, window.WindowDate);
        Assert.Equal(context.UserTimeZoneId, window.UserTimeZoneId);
        Assert.Equal(context.TargetLocalTime, window.ScheduledLocalTime);
    }

    [Fact]
    public void SchedulerContract_EvaluatesDueState_AsBoolean()
    {
        var window = new MorningSummaryWindow(
            new DateOnly(2026, 6, 22),
            "Europe/Madrid",
            new TimeOnly(9, 0),
            new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero));

        IMorningSummaryScheduler scheduler = new FakeMorningSummaryScheduler();

        var beforeDue = scheduler.IsWindowDue(window, new DateTimeOffset(2026, 6, 22, 6, 59, 0, TimeSpan.Zero));
        var atDue = scheduler.IsWindowDue(window, new DateTimeOffset(2026, 6, 22, 7, 0, 0, TimeSpan.Zero));

        Assert.False(beforeDue);
        Assert.True(atDue);
    }

    [Fact]
    public void ReaderContract_IsDefinedWithoutImplementation_WithExpectedSignature()
    {
        var method = typeof(IWorkItemReader).GetMethod(nameof(IWorkItemReader.ReadForWindowAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyList<WorkItem>>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(MorningSummaryQuery), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        var applicationImplementations = typeof(IWorkItemReader)
            .Assembly
            .GetTypes()
            .Where(type => typeof(IWorkItemReader).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        Assert.Empty(applicationImplementations);
    }

    private static WorkItem CreateWorkItem(string externalId, string title)
    {
        return new WorkItem(
            externalId,
            title,
            "teams",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.High,
            new Dictionary<string, string>());
    }

    private sealed class FakeMorningSummaryComposer : IMorningSummaryComposer
    {
        public Task<MorningSummary> ComposeAsync(MorningSummaryRequest request, CancellationToken ct)
        {
            var rankedEntry = new RankedWorkItem(
                1,
                CreateWorkItem("external-1", "Top item"),
                0.90,
                new RankingExplanation(
                [
                    new RankingFactorContribution(RankingFactor.Impact, 0.60, "High impact")
                ]));

            var payload = new MorningSummary(
                request.UserId,
                request.Window,
                new DateTimeOffset(2026, 6, 22, 7, 1, 0, TimeSpan.Zero),
                [rankedEntry]);

            return Task.FromResult(payload);
        }
    }

    private sealed class FakeMorningSummaryScheduler : IMorningSummaryScheduler
    {
        public MorningSummaryWindow ResolveWindow(MorningSummaryScheduleContext context)
        {
            var scheduledInstant = new DateTimeOffset(
                context.WindowDate.Year,
                context.WindowDate.Month,
                context.WindowDate.Day,
                context.TargetLocalTime.Hour,
                context.TargetLocalTime.Minute,
                context.TargetLocalTime.Second,
                TimeSpan.Zero);

            return new MorningSummaryWindow(
                context.WindowDate,
                context.UserTimeZoneId,
                context.TargetLocalTime,
                scheduledInstant);
        }

        public bool IsWindowDue(MorningSummaryWindow window, DateTimeOffset evaluationInstant)
        {
            return evaluationInstant >= window.ScheduledInstantUtc;
        }
    }
}
