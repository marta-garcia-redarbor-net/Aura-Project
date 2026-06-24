using Aura.Infrastructure.Adapters.GraphConnector;

namespace Aura.UnitTests.GraphConnector;

public class GraphConnectorOptionsTests
{
    [Fact]
    public void RedirectUri_DefaultIsNull()
    {
        var options = new GraphConnectorOptions();

        Assert.Null(options.RedirectUri);
    }

    [Fact]
    public void RedirectUri_CanBeSet()
    {
        var options = new GraphConnectorOptions { RedirectUri = "https://localhost:5001/signin-oidc" };

        Assert.Equal("https://localhost:5001/signin-oidc", options.RedirectUri);
    }

    [Fact]
    public void Scopes_DefaultIsNull()
    {
        var options = new GraphConnectorOptions();

        Assert.Null(options.Scopes);
    }

    [Fact]
    public void Scopes_CanBeSetToMultiple()
    {
        var scopes = new[] { "Mail.Read", "Chat.Read", "User.Read" };
        var options = new GraphConnectorOptions { Scopes = scopes };

        Assert.Equal(3, options.Scopes!.Length);
        Assert.Contains("Mail.Read", options.Scopes);
    }
}
