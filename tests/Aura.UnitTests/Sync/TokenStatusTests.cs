using Aura.Application.Models;

namespace Aura.UnitTests.Sync;

public class TokenStatusTests
{
    [Fact]
    public void TokenStatus_ValidToken_HasExpectedState()
    {
        var scopes = new[] { "Mail.Read", "Chat.Read" };
        var status = new TokenStatus(IsValid: true, RequiresReauth: false, Scopes: scopes);

        Assert.True(status.IsValid);
        Assert.False(status.RequiresReauth);
        Assert.Equal(2, status.Scopes.Count);
        Assert.Contains("Mail.Read", status.Scopes);
        Assert.Contains("Chat.Read", status.Scopes);
    }

    [Fact]
    public void TokenStatus_ExpiredToken_RequiresReauth()
    {
        var status = new TokenStatus(IsValid: false, RequiresReauth: true, Scopes: []);

        Assert.False(status.IsValid);
        Assert.True(status.RequiresReauth);
        Assert.Empty(status.Scopes);
    }
}
