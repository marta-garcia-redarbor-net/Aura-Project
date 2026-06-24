using Aura.Infrastructure.Adapters.Connectors.Teams;

namespace Aura.UnitTests.GraphConnector;

/// <summary>
/// Tests that TeamsWorkItemMapper maps the new fields (Sender, BodyPreview, WebUrl)
/// into WorkItem metadata as deepLink, snippet, and sender.
/// </summary>
public class TeamsWorkItemMapperNewFieldsTests
{
    private readonly TeamsWorkItemMapper _mapper = new();

    [Fact]
    public void TryMap_WithAllNewFields_MapsDeepLinkSnippetSender()
    {
        var dto = new TeamsMessageDto
        {
            ExternalId = "msg-100",
            Title = "Planning",
            Source = "chats",
            Priority = "high",
            TeamId = "team-a",
            ChannelId = "ch-1",
            MessageUrl = "https://teams/legacy",
            Sender = "Alice Smith",
            BodyPreview = "Let's discuss the new architecture",
            WebUrl = "https://teams.microsoft.com/deep/msg-100"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("Alice Smith", workItem!.Metadata["teams.sender"]);
        Assert.Equal("Let's discuss the new architecture", workItem.Metadata["teams.snippet"]);
        Assert.Equal("https://teams.microsoft.com/deep/msg-100", workItem.Metadata["teams.deepLink"]);
    }

    [Fact]
    public void TryMap_WithNullNewFields_OmitsFromMetadata()
    {
        var dto = new TeamsMessageDto
        {
            ExternalId = "msg-101",
            Title = "Quick sync",
            Source = "chats",
            Priority = "medium"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.False(workItem!.Metadata.ContainsKey("teams.sender"));
        Assert.False(workItem.Metadata.ContainsKey("teams.snippet"));
        Assert.False(workItem.Metadata.ContainsKey("teams.deepLink"));
    }

    [Fact]
    public void TryMap_WebUrl_OverridesLegacyMessageUrl_ForDeepLink()
    {
        var dto = new TeamsMessageDto
        {
            ExternalId = "msg-102",
            Title = "Priority",
            Source = "chats",
            Priority = "high",
            MessageUrl = "https://legacy-url",
            WebUrl = "https://new-deep-link"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        // WebUrl should be the deep link, MessageUrl preserved in its own key
        Assert.Equal("https://new-deep-link", workItem!.Metadata["teams.deepLink"]);
        Assert.Equal("https://legacy-url", workItem.Metadata["teams.messageUrl"]);
    }
}
