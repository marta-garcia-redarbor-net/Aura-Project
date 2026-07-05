using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class PrioritySummaryCardsRenderingTests : TestContext
{
    private static UpcomingMeetingResponse CreateMeeting(
        string title,
        DateTimeOffset start,
        DateTimeOffset end,
        string? location = null,
        bool isOnline = false,
        string? joinUrl = null)
    {
        return new UpcomingMeetingResponse(
            Id: Guid.NewGuid().ToString(),
            Title: title,
            StartUtc: start,
            EndUtc: end,
            IsOnlineMeeting: isOnline,
            JoinUrl: joinUrl,
            Organizer: null,
            Location: location);
    }

    private void RegisterService(List<PrioritySummaryCard> cards)
    {
        var service = Substitute.For<IPrioritySummaryService>();
        service.GetCardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cards));
        service.FormatTimeRange(Arg.Any<UpcomingMeetingResponse>())
            .Returns(ci =>
            {
                var ev = ci.Arg<UpcomingMeetingResponse>();
                var start = ev.StartUtc.ToLocalTime();
                var end = ev.EndUtc.ToLocalTime();
                return $"{start:h:mm} - {end:h:mm}";
            });
        service.GetEventStatus(Arg.Any<UpcomingMeetingResponse>())
            .Returns(ci =>
            {
                var ev = ci.Arg<UpcomingMeetingResponse>();
                var now = DateTimeOffset.UtcNow;
                if (ev.EndUtc < now) return "past";
                if (ev.StartUtc <= now) return "current";
                return "upcoming";
            });
        Services.AddSingleton<IPrioritySummaryService>(service);
    }

    [Fact]
    public void RendersCalendarItems_WhenScheduleCardHasEvents()
    {
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting("Daily Standup", now.AddHours(1), now.AddHours(2),
            location: "Room A", isOnline: true, joinUrl: "https://meet.example.com/abc");

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, [meeting])
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var scheduleCard = cut.Find("[data-source='schedule']");
        Assert.Contains("Daily Standup", scheduleCard.TextContent);
        Assert.Contains("Room A", scheduleCard.TextContent);
    }

    [Fact]
    public void RendersJoinLink_WhenEventIsOnlineMeeting()
    {
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting("Sprint Planning", now.AddHours(2), now.AddHours(3),
            location: "Online", isOnline: true, joinUrl: "https://teams.microsoft.com/l/meetup-join/123");

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, [meeting])
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var joinLink = cut.Find("a[href='https://teams.microsoft.com/l/meetup-join/123']");
        Assert.Equal("Join", joinLink.TextContent);
    }

    [Fact]
    public void DoesNotRenderJoinLink_WhenEventIsNotOnline()
    {
        var now = DateTimeOffset.UtcNow;
        var meeting = CreateMeeting("1:1 Meeting", now.AddHours(3), now.AddHours(4),
            location: "Office 301", isOnline: false);

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, [meeting])
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        Assert.DoesNotContain("schedule-item__join", cut.Markup);
    }

    [Fact]
    public void RendersFooter_WhenCardHasMoreThan3Items()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => new InboxItemPreviewResponse(
                Title: $"Item {i}",
                Source: "messages",
                RelativeTimestamp: $"{i}m ago",
                Score: i,
                SuggestedAction: "Review and respond")
            {
                Sender = $"user{i}",
                Snippet = $"snippet {i}",
                PriorityHint = "High"
            })
            .ToList();

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", items, null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, [])
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var teamsCard = cut.Find("[data-source='teams']");
        Assert.Contains("View all 5 items", teamsCard.TextContent);
        Assert.Contains("Open Teams", teamsCard.TextContent);
    }

    [Fact]
    public void RendersCalendarFooter_WhenScheduleHasMoreThan3Events()
    {
        var now = DateTimeOffset.UtcNow;
        var events = Enumerable.Range(1, 5)
            .Select(i => CreateMeeting($"Meeting {i}", now.AddHours(i), now.AddHours(i + 1)))
            .ToList();

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, events)
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var scheduleCard = cut.Find("[data-source='schedule']");
        Assert.Contains("View all 5 meetings", scheduleCard.TextContent);
        Assert.Contains("Open Calendar", scheduleCard.TextContent);
    }

    [Fact]
    public void RendersPrMiniTable_WhenCardHasPrItems()
    {
        // Arrange
        var prItems = new List<PrPreviewItemResponse>
        {
            new(
                Title: "Fix: SSO redirect",
                PrDisplayName: "#139 Fix: SSO redirect",
                BranchName: "main",
                BuildStatus: "passing",
                ReviewApprovals: 1,
                ReviewRequired: 2,
                ReviewChangesRequested: 0,
                Author: "David Martínez",
                UpdatedAt: DateTimeOffset.UtcNow.AddHours(-3),
                RelativeTimestamp: "3h ago",
                SourceLink: "https://dev.azure.com/pr/139",
                IsDraft: false,
                Priority: "critical")
        };

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Schedule Today", "calendar_today", "schedule", "EVENTS",
                "meetings", "Open Calendar", "https://outlook.office.com/calendar/view/day",
                "/calendar/day", null, []),
            new PrioritySummaryCard("Pull Requests", "account_tree", "pr-review", "PENDING",
                "PRs", "View All Repositories", "https://redarbor.visualstudio.com/",
                "/pull-requests", null, null)
            {
                IsPrCard = true,
                PrItems = prItems
            }
        ]);

        // Act
        var cut = RenderComponent<PrioritySummaryCards>();

        // Assert
        var prCard = cut.Find("[data-source='pr-review']");
        Assert.Contains("#139 Fix: SSO redirect", prCard.TextContent);
        Assert.Contains("1/2 Approved", prCard.TextContent);
        Assert.NotNull(cut.Find("[data-testid='pr-mini-table']"));
    }

    [Fact]
    public void RendersTopPriorityBadgeNextToTeamsCardTitle_WhenGroupContainsTopItem()
    {
        var teamsItems = new List<InboxItemPreviewResponse>
        {
            new("A", "messages", "1m ago", 1, "Review") { PriorityScore = 95 },
            new("B", "messages", "2m ago", 1, "Review") { PriorityScore = 70 }
        };

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", teamsItems, null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Pull Requests", "account_tree", "pr-review", "PENDING",
                "PRs", "View All Repositories", "https://redarbor.visualstudio.com/",
                "/pull-requests", null, null)
            {
                IsPrCard = true,
                PrItems =
                [
                    new PrPreviewItemResponse(
                        "Fix", "#1 Fix", "main", "passing", 1, 1, 0, "dev",
                        DateTimeOffset.UtcNow.AddMinutes(-5), "5m ago", "https://dev.azure.com/pr/1", false, "high")
                ]
            }
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var teamsCard = cut.Find("[data-source='teams']");
        var badge = teamsCard.QuerySelector("[data-testid='priority-card-top-badge']");
        Assert.NotNull(badge);
        Assert.Equal("Top priority", badge!.GetAttribute("title"));
    }

    [Fact]
    public void RendersTopPriorityBadgeNextToOutlookCardTitle_WhenGroupContainsTopItem()
    {
        var outlookItems = new List<InboxItemPreviewResponse>
        {
            new("Email A", "inbox", "1m ago", 1, "Review") { PriorityScore = 92 },
            new("Email B", "inbox", "2m ago", 1, "Review") { PriorityScore = 65 }
        };

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", outlookItems, null),
            new PrioritySummaryCard("Pull Requests", "account_tree", "pr-review", "PENDING",
                "PRs", "View All Repositories", "https://redarbor.visualstudio.com/",
                "/pull-requests", null, null)
            {
                IsPrCard = true,
                PrItems =
                [
                    new PrPreviewItemResponse(
                        "Fix", "#1 Fix", "main", "passing", 1, 1, 0, "dev",
                        DateTimeOffset.UtcNow.AddMinutes(-5), "5m ago", "https://dev.azure.com/pr/1", false, "medium")
                ]
            }
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var outlookCard = cut.Find("[data-source='outlook']");
        var badge = outlookCard.QuerySelector("[data-testid='priority-card-top-badge']");
        Assert.NotNull(badge);
        Assert.Equal("Top priority", badge!.GetAttribute("title"));
    }

    [Fact]
    public void RendersTopPriorityBadgeNextToPrCardTitle_WhenPrContainsTopItem()
    {
        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "NEW", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", [], null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "UNREAD", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Pull Requests", "account_tree", "pr-review", "PENDING",
                "PRs", "View All Repositories", "https://redarbor.visualstudio.com/",
                "/pull-requests", null, null)
            {
                IsPrCard = true,
                PrItems =
                [
                    new PrPreviewItemResponse(
                        "Critical Fix", "#9 Critical Fix", "main", "passing", 1, 1, 0, "dev",
                        DateTimeOffset.UtcNow.AddMinutes(-2), "2m ago", "https://dev.azure.com/pr/9", false, "critical")
                ]
            }
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var prCard = cut.Find("[data-source='pr-review']");
        var badge = prCard.QuerySelector("[data-testid='priority-card-top-badge']");
        Assert.NotNull(badge);
        Assert.Equal("Top priority", badge!.GetAttribute("title"));
    }

    [Fact]
    public void RendersHighPriorityCounterNextToCardCount_WithAriaLabel()
    {
        var teamsItems = new List<InboxItemPreviewResponse>
        {
            new("A", "messages", "1m ago", 1, "Review") { PriorityScore = 95 },
            new("B", "messages", "2m ago", 1, "Review") { PriorityScore = 75 },
            new("C", "messages", "3m ago", 1, "Review") { PriorityScore = 40 }
        };

        RegisterService([
            new PrioritySummaryCard("Teams Mentions", "groups", "teams", "new", "items",
                "Open Teams", "https://teams.microsoft.com", "/teams", teamsItems, null),
            new PrioritySummaryCard("Outlook", "mail", "outlook", "new", "items",
                "Open Outlook", "https://outlook.office.com", "/outlook", [], null),
            new PrioritySummaryCard("Pull Requests", "account_tree", "pr-review", "new", "PRs",
                "View All Repositories", "https://redarbor.visualstudio.com/", "/pull-requests", null, null)
            {
                IsPrCard = true,
                PrItems =
                [
                    new PrPreviewItemResponse(
                        "Critical", "#1 Critical", "main", "passing", 1, 1, 0, "dev",
                        DateTimeOffset.UtcNow.AddMinutes(-1), "1m ago", "https://dev.azure.com/pr/1", false, "critical")
                ]
            }
        ]);

        var cut = RenderComponent<PrioritySummaryCards>();

        var teamsCard = cut.Find("[data-source='teams']");
        var highBadge = teamsCard.QuerySelector("[data-testid='priority-card-high-count']");
        Assert.NotNull(highBadge);
        Assert.Equal("2 high priority", highBadge!.TextContent.Trim());
        Assert.Equal("2 high priority", highBadge.GetAttribute("aria-label"));
    }
}
