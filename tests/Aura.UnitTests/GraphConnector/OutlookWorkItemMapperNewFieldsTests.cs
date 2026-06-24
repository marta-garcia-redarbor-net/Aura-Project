using Aura.Infrastructure.Adapters.Connectors.Outlook;

namespace Aura.UnitTests.GraphConnector;

/// <summary>
/// Tests that OutlookWorkItemMapper maps the new WebLink field
/// into WorkItem metadata as deepLink, and snippet from BodyPreview.
/// </summary>
public class OutlookWorkItemMapperNewFieldsTests
{
    private readonly OutlookWorkItemMapper _mapper = new();

    [Fact]
    public void TryMap_WithWebLink_MapsDeepLink()
    {
        var dto = new OutlookEmailDto
        {
            ExternalId = "mail-200",
            Subject = "Quarterly review",
            Importance = "High",
            SenderAddress = "ceo@acme.dev",
            BodyPreview = "Please review the attached report",
            WebLink = "https://outlook.office.com/mail/AAA"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("https://outlook.office.com/mail/AAA", workItem!.Metadata["outlook.deepLink"]);
        Assert.Equal("Please review the attached report", workItem.Metadata["outlook.snippet"]);
    }

    [Fact]
    public void TryMap_WithNullWebLink_OmitsDeepLink()
    {
        var dto = new OutlookEmailDto
        {
            ExternalId = "mail-201",
            Subject = "Status update",
            Importance = "Normal",
            SenderAddress = "dev@acme.dev",
            BodyPreview = "All good"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.False(workItem!.Metadata.ContainsKey("outlook.deepLink"));
    }

    [Fact]
    public void TryMap_BodyPreview_MapsAsSnippet()
    {
        var dto = new OutlookEmailDto
        {
            ExternalId = "mail-202",
            Subject = "Design review",
            Importance = "Normal",
            SenderAddress = "arch@acme.dev",
            BodyPreview = "The hexagonal architecture approach looks solid"
        };

        var result = _mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("The hexagonal architecture approach looks solid", workItem.Metadata["outlook.snippet"]);
    }
}
