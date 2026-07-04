using Aura.Domain.WorkItems;

namespace Aura.UnitTests.Domain;

public class NotificationOutboxEntryTests
{
    [Fact]
    public void EnqueueCtor_WithFullVerdict_PopulatesAllFields()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var entry = new NotificationOutboxEntry(
            workItemId: id,
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Urgent message from VIP",
            priority: 5.0,
            triggerRule: "vip_sender",
            explanation: "VIP sender detected — high urgency",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[{\"ruleName\":\"vip_sender\",\"matched\":true,\"score\":9.0,\"confidence\":0.95,\"reason\":\"VIP sender detected\"}]");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(id, entry.WorkItemId);
        Assert.Equal("user-abc", entry.UserId);
        Assert.Equal("TeamsMessage", entry.SourceType);
        Assert.Equal("Urgent message from VIP", entry.Title);
        Assert.Equal(5.0, entry.Priority);
        Assert.Equal("vip_sender", entry.TriggerRule);
        Assert.Equal("VIP sender detected — high urgency", entry.Explanation);
        Assert.Equal("InterruptNow", entry.Decision);
        Assert.Equal("user-abc", entry.TargetUserId);
        Assert.Equal("[{\"ruleName\":\"vip_sender\",\"matched\":true,\"score\":9.0,\"confidence\":0.95,\"reason\":\"VIP sender detected\"}]", entry.RuleResults);
    }

    [Fact]
    public void EnqueueCtor_WithoutVerdictFields_AllAreNull()
    {
        var id = Guid.NewGuid();

        var entry = new NotificationOutboxEntry(
            workItemId: id,
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Normal message",
            priority: 3.0);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(id, entry.WorkItemId);
        Assert.Null(entry.TriggerRule);
        Assert.Null(entry.Explanation);
        Assert.Null(entry.Decision);
        Assert.Null(entry.TargetUserId);
        Assert.Null(entry.RuleResults);
    }

    [Fact]
    public void PersistedCtor_WithVerdictFields_PopulatesAllFields()
    {
        var id = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var entry = new NotificationOutboxEntry(
            id: id,
            workItemId: workItemId,
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Urgent message",
            priority: 5.0,
            triggerRule: "vip_sender",
            createdAt: createdAt,
            dispatchedAt: null,
            explanation: "VIP sender detected",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[]");

        Assert.Equal(id, entry.Id);
        Assert.Equal(workItemId, entry.WorkItemId);
        Assert.Equal("user-abc", entry.UserId);
        Assert.Equal("TeamsMessage", entry.SourceType);
        Assert.Equal("Urgent message", entry.Title);
        Assert.Equal(5.0, entry.Priority);
        Assert.Equal("vip_sender", entry.TriggerRule);
        Assert.Equal(createdAt, entry.CreatedAt);
        Assert.Null(entry.DispatchedAt);
        Assert.Equal("VIP sender detected", entry.Explanation);
        Assert.Equal("InterruptNow", entry.Decision);
        Assert.Equal("user-abc", entry.TargetUserId);
        Assert.Equal("[]", entry.RuleResults);
    }

    [Fact]
    public void PersistedCtor_WithoutVerdictFields_AllAreNull()
    {
        var id = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var dispatchedAt = DateTimeOffset.UtcNow;

        var entry = new NotificationOutboxEntry(
            id: id,
            workItemId: workItemId,
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Normal message",
            priority: 3.0,
            triggerRule: null,
            createdAt: createdAt,
            dispatchedAt: dispatchedAt);

        Assert.Equal(id, entry.Id);
        Assert.Equal(workItemId, entry.WorkItemId);
        Assert.Equal("user-abc", entry.UserId);
        Assert.Equal(3.0, entry.Priority);
        Assert.Null(entry.TriggerRule);
        Assert.Equal(createdAt, entry.CreatedAt);
        Assert.Equal(dispatchedAt, entry.DispatchedAt);
        Assert.Null(entry.Explanation);
        Assert.Null(entry.Decision);
        Assert.Null(entry.TargetUserId);
        Assert.Null(entry.RuleResults);
    }

    [Fact]
    public void MarkDispatched_SetsDispatchedAt()
    {
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Test",
            priority: 1.0);

        Assert.Null(entry.DispatchedAt);

        entry.MarkDispatched();

        Assert.NotNull(entry.DispatchedAt);
        Assert.True(entry.DispatchedAt!.Value > DateTimeOffset.UtcNow.AddSeconds(-5));
    }
}
