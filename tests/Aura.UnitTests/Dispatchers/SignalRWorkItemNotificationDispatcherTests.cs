using Aura.Api.Adapters;
using Aura.Api.Hubs;
using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Dispatchers;

public class SignalRWorkItemNotificationDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithVerdictFields_IncludesAuditFieldsInPayload()
    {
        var hubContext = Substitute.For<IHubContext<AlertHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Group(Arg.Any<string>()).Returns(clientProxy);

        var dispatcher = new SignalRWorkItemNotificationDispatcher(
            hubContext,
            Substitute.For<ILogger<SignalRWorkItemNotificationDispatcher>>());

        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Test alert",
            priority: 5.0,
            triggerRule: "vip_sender",
            explanation: "VIP sender detected",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[{\"RuleName\":\"vip_sender\"}]");

        var verdict = new InterruptionVerdict(
            InterruptionDecision.InterruptNow,
            new EvaluationReport([]),
            triggerRule: "vip_sender",
            explanation: "VIP sender detected",
            targetUserId: "user-abc");

        await dispatcher.DispatchAsync(entry, verdict, CancellationToken.None);

        await clientProxy.Received(1).SendCoreAsync(
            "UrgentWorkItem",
            Arg.Is<object?[]?>(args => args != null && args.Length > 0 && PayloadContainsAuditFields(args[0]!)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithVerdictFields_IncludesExistingFields()
    {
        var hubContext = Substitute.For<IHubContext<AlertHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Group(Arg.Any<string>()).Returns(clientProxy);

        var dispatcher = new SignalRWorkItemNotificationDispatcher(
            hubContext,
            Substitute.For<ILogger<SignalRWorkItemNotificationDispatcher>>());

        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Test alert",
            priority: 5.0,
            triggerRule: "vip_sender",
            explanation: "VIP sender detected",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[]");

        var verdict = new InterruptionVerdict(
            InterruptionDecision.InterruptNow,
            new EvaluationReport([]),
            triggerRule: "vip_sender",
            explanation: "VIP sender detected",
            targetUserId: "user-abc");

        await dispatcher.DispatchAsync(entry, verdict, CancellationToken.None);

        await clientProxy.Received(1).SendCoreAsync(
            "UrgentWorkItem",
            Arg.Is<object?[]?>(args => args != null && args.Length > 0 && PayloadContainsExistingFields(args[0]!)),
            Arg.Any<CancellationToken>());
    }

    private static bool PayloadContainsAuditFields(object payload)
    {
        var dict = ToDictionary(payload);
        return dict.ContainsKey("Explanation")
            && "VIP sender detected".Equals(dict["Explanation"]?.ToString())
            && dict.ContainsKey("Decision")
            && "InterruptNow".Equals(dict["Decision"]?.ToString())
            && dict.ContainsKey("TargetUserId")
            && "user-abc".Equals(dict["TargetUserId"]?.ToString())
            && dict.ContainsKey("RuleResults")
            && "[{\"RuleName\":\"vip_sender\"}]".Equals(dict["RuleResults"]?.ToString());
    }

    private static bool PayloadContainsExistingFields(object payload)
    {
        var dict = ToDictionary(payload);
        return dict.ContainsKey("Id")
            && dict.ContainsKey("Title")
            && "Test alert".Equals(dict["Title"]?.ToString())
            && dict.ContainsKey("SourceType")
            && dict.ContainsKey("Priority")
            && dict.ContainsKey("TriggerRule")
            && dict.ContainsKey("Reason");
    }

    private static Dictionary<string, object?> ToDictionary(object payload)
    {
        return payload.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(payload));
    }
}
