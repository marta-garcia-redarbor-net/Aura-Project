using Aura.Infrastructure.Adapters.Connectors.Outlook;

namespace Aura.UnitTests.Ingestion.Outlook;

public class OutlookEmailDtoTests
{
    [Fact]
    public void InitProperties_AssignsProvidedValues()
    {
        var receivedAt = new DateTimeOffset(2026, 06, 21, 9, 30, 0, TimeSpan.Zero);

        var dto = new OutlookEmailDto
        {
            ExternalId = "mail-1001",
            Subject = "Incident update",
            Importance = "High",
            SenderAddress = "ceo@aura.dev",
            BodyPreview = "Need update before EOD",
            ReceivedDateTime = receivedAt,
            CorrelationId = "corr-1001",
            ConversationId = "conv-1001"
        };

        Assert.Equal("mail-1001", dto.ExternalId);
        Assert.Equal("Incident update", dto.Subject);
        Assert.Equal("High", dto.Importance);
        Assert.Equal("ceo@aura.dev", dto.SenderAddress);
        Assert.Equal("Need update before EOD", dto.BodyPreview);
        Assert.Equal(receivedAt, dto.ReceivedDateTime);
        Assert.Equal("corr-1001", dto.CorrelationId);
        Assert.Equal("conv-1001", dto.ConversationId);
    }
}
