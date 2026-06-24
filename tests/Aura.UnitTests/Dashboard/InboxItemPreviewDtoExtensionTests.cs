using System.Text.Json;
using Aura.Application.Models;

namespace Aura.UnitTests.Dashboard;

public class InboxItemPreviewDtoExtensionTests
{
    [Fact]
    public void NewOptionalFields_DefaultToNull()
    {
        var dto = new InboxItemPreviewDto("Title", "teams", "2m ago", 0.8, "Review");

        Assert.Null(dto.Sender);
        Assert.Null(dto.Snippet);
        Assert.Null(dto.DeepLink);
        Assert.Null(dto.PriorityHint);
        Assert.Null(dto.SyncState);
    }

    [Fact]
    public void NewOptionalFields_CanBeSetViaInitOnly()
    {
        var dto = new InboxItemPreviewDto("Title", "outlook", "5m ago", 0.9, "Reply")
        {
            Sender = "ceo@contoso.com",
            Snippet = "Please review the quarterly...",
            DeepLink = "https://outlook.office.com/mail/id/abc",
            PriorityHint = "high",
            SyncState = "synced"
        };

        Assert.Equal("ceo@contoso.com", dto.Sender);
        Assert.Equal("Please review the quarterly...", dto.Snippet);
        Assert.Equal("https://outlook.office.com/mail/id/abc", dto.DeepLink);
        Assert.Equal("high", dto.PriorityHint);
        Assert.Equal("synced", dto.SyncState);
    }

    [Fact]
    public void Json_NullOptionalFields_OmittedByDefault()
    {
        var dto = new InboxItemPreviewDto("Title", "teams", "2m ago", 0.8, "Review");
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(dto, options);

        Assert.DoesNotContain("Sender", json);
        Assert.DoesNotContain("Snippet", json);
        Assert.DoesNotContain("DeepLink", json);
        Assert.DoesNotContain("PriorityHint", json);
        Assert.DoesNotContain("SyncState", json);
    }

    [Fact]
    public void Json_WithOptionalFields_RoundTrips()
    {
        var dto = new InboxItemPreviewDto("Title", "outlook", "5m ago", 0.9, "Reply")
        {
            Sender = "user@example.com",
            Snippet = "Hello world",
            DeepLink = "https://example.com/mail/123",
            PriorityHint = "medium",
            SyncState = "synced"
        };

        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<InboxItemPreviewDto>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("user@example.com", deserialized!.Sender);
        Assert.Equal("Hello world", deserialized.Snippet);
        Assert.Equal("https://example.com/mail/123", deserialized.DeepLink);
        Assert.Equal("medium", deserialized.PriorityHint);
        Assert.Equal("synced", deserialized.SyncState);
    }
}
