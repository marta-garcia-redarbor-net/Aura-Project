using Aura.Infrastructure.Adapters.Connectors.Teams;

namespace Aura.UnitTests.GraphConnector;

public class TeamsMessageDtoTests
{
    [Fact]
    public void LastMessageReadAt_InitOnlyProperty_CanBeSet()
    {
        var dto = new TeamsMessageDto
        {
            LastMessageReadAt = new DateTimeOffset(2026, 6, 30, 14, 0, 0, TimeSpan.Zero)
        };

        Assert.NotNull(dto.LastMessageReadAt);
        Assert.Equal(new DateTimeOffset(2026, 6, 30, 14, 0, 0, TimeSpan.Zero), dto.LastMessageReadAt!.Value);
    }

    [Fact]
    public void LastMessageAt_InitOnlyProperty_CanBeSet()
    {
        var dto = new TeamsMessageDto
        {
            LastMessageAt = new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.Zero)
        };

        Assert.NotNull(dto.LastMessageAt);
        Assert.Equal(new DateTimeOffset(2026, 6, 30, 15, 0, 0, TimeSpan.Zero), dto.LastMessageAt!.Value);
    }

    [Fact]
    public void UnreadCount_InitOnlyProperty_CanBeSet()
    {
        var dto = new TeamsMessageDto
        {
            UnreadCount = 5
        };

        Assert.Equal(5, dto.UnreadCount);
    }

    [Fact]
    public void UnreadCount_Default_IsZero()
    {
        var dto = new TeamsMessageDto();

        Assert.Equal(0, dto.UnreadCount);
    }
}
