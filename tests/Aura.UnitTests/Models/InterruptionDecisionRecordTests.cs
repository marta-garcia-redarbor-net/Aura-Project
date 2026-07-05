namespace Aura.UnitTests.Models;

public class InterruptionDecisionRecordTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var record = new Aura.Application.Models.InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "Urgent PR review",
            SourceType: "pr-review",
            Decision: "INTERRUPT",
            PriorityScore: 88,
            Explanation: "High priority item during receptive window",
            Timestamp: now,
            FocusState: "WindowOfOpportunity");

        Assert.Equal("Urgent PR review", record.Title);
        Assert.Equal("pr-review", record.SourceType);
        Assert.Equal("INTERRUPT", record.Decision);
        Assert.Equal(88, record.PriorityScore);
        Assert.Equal("WindowOfOpportunity", record.FocusState);
        Assert.Equal(now, record.Timestamp);
    }

    [Fact]
    public void Constructor_AcceptsNullPriorityScore()
    {
        var record = new Aura.Application.Models.InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "Test",
            SourceType: "email",
            Decision: "QUEUE",
            PriorityScore: null,
            Explanation: "No score",
            Timestamp: DateTimeOffset.UtcNow,
            FocusState: "Away");

        Assert.Null(record.PriorityScore);
    }

    [Fact]
    public void Constructor_AllowsAllVerdictTypes()
    {
        var verdicts = new[] { "INTERRUPT", "QUEUE", "DEFER" };

        foreach (var verdict in verdicts)
        {
            var record = new Aura.Application.Models.InterruptionDecisionRecord(
                WorkItemId: Guid.NewGuid(),
                Title: "Test",
                SourceType: "email",
                Decision: verdict,
                PriorityScore: 50,
                Explanation: verdict,
                Timestamp: DateTimeOffset.UtcNow,
                FocusState: "DeepWork");

            Assert.Equal(verdict, record.Decision);
        }
    }
}
