using System.Text.Json;
using Aura.UI.Models;

namespace Aura.UnitTests.Dashboard;

public class DashboardPreviewResponseOptionalFieldsTests
{
    [Fact]
    public void InboxItemPreviewResponse_NewOptionalFields_DefaultToNull()
    {
        var item = new InboxItemPreviewResponse("Title", "teams", "2m ago", 0.8, "Review");

        Assert.Null(item.Sender);
        Assert.Null(item.Snippet);
        Assert.Null(item.DeepLink);
        Assert.Null(item.PriorityHint);
        Assert.Null(item.SyncState);
    }

    [Fact]
    public void InboxItemPreviewResponse_NewOptionalFields_CanBeSetViaInitOnly()
    {
        var item = new InboxItemPreviewResponse("Title", "outlook", "5m ago", 0.9, "Reply")
        {
            Sender = "ceo@contoso.com",
            Snippet = "Please review the quarterly...",
            DeepLink = "https://outlook.office.com/mail/id/abc",
            PriorityHint = "high",
            SyncState = "synced"
        };

        Assert.Equal("ceo@contoso.com", item.Sender);
        Assert.Equal("Please review the quarterly...", item.Snippet);
        Assert.Equal("https://outlook.office.com/mail/id/abc", item.DeepLink);
        Assert.Equal("high", item.PriorityHint);
        Assert.Equal("synced", item.SyncState);
    }

    [Fact]
    public void Json_NullOptionalFields_OmittedByDefault()
    {
        var item = new InboxItemPreviewResponse("Title", "teams", "2m ago", 0.8, "Review");
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(item, options);

        Assert.DoesNotContain("Sender", json);
        Assert.DoesNotContain("Snippet", json);
        Assert.DoesNotContain("DeepLink", json);
        Assert.DoesNotContain("PriorityHint", json);
        Assert.DoesNotContain("SyncState", json);
    }

    [Fact]
    public void Json_WithOptionalFields_RoundTrips()
    {
        var item = new InboxItemPreviewResponse("Title", "outlook", "5m ago", 0.9, "Reply")
        {
            Sender = "user@example.com",
            Snippet = "Hello world",
            DeepLink = "https://example.com/mail/123",
            PriorityHint = "medium",
            SyncState = "synced"
        };

        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<InboxItemPreviewResponse>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("user@example.com", deserialized!.Sender);
        Assert.Equal("Hello world", deserialized.Snippet);
        Assert.Equal("https://example.com/mail/123", deserialized.DeepLink);
        Assert.Equal("medium", deserialized.PriorityHint);
        Assert.Equal("synced", deserialized.SyncState);
    }

    [Fact]
    public void DashboardPreviewResponse_WithOptionalFields_DeserializesFromJson()
    {
        var preview = new DashboardPreviewResponse(
            [
                new InboxSourceGroupResponse("teams",
                [
                    new InboxItemPreviewResponse("Meeting", "teams", "1h ago", 85.0, "Join")
                    {
                        Sender = "alice@contoso.com",
                        Snippet = "Let's sync on the project",
                        DeepLink = "https://teams.microsoft.com/l/message/123",
                        PriorityHint = "high",
                        SyncState = "synced"
                    }
                ])
            ],
            []);

        var json = JsonSerializer.Serialize(preview);
        var deserialized = JsonSerializer.Deserialize<DashboardPreviewResponse>(json);

        Assert.NotNull(deserialized);
        var item = deserialized!.InboxGroups[0].Items[0];
        Assert.Equal("alice@contoso.com", item.Sender);
        Assert.Equal("Let's sync on the project", item.Snippet);
        Assert.Equal("https://teams.microsoft.com/l/message/123", item.DeepLink);
        Assert.Equal("high", item.PriorityHint);
        Assert.Equal("synced", item.SyncState);
    }
}
