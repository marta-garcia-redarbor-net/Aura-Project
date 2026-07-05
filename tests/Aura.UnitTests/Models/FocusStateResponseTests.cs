namespace Aura.UnitTests.Models;

public class FocusStateResponseTests
{
    [Fact]
    public void CanCreate_WithDefaultOverride()
    {
        var response = new Aura.Application.Models.FocusStateResponse
        {
            State = "DeepWork",
            IsOverridden = false,
            UserId = "user-123"
        };

        Assert.Equal("DeepWork", response.State);
        Assert.False(response.IsOverridden);
        Assert.Equal("user-123", response.UserId);
    }

    [Fact]
    public void CanCreate_WithExplicitOverride()
    {
        var response = new Aura.Application.Models.FocusStateResponse
        {
            State = "DeepWork",
            IsOverridden = true,
            UserId = "user-123"
        };

        Assert.True(response.IsOverridden);
    }
}
