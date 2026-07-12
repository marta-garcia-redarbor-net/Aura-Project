using Aura.Infrastructure.Adapters.GraphConnector;

namespace Aura.UnitTests.GraphConnector;

public class GraphConnectorOptionsTests
{
    [Fact]
    public void IsProductionReady_WhenEnabledAndRequiredFieldsPresent_ReturnsTrue()
    {
        var options = new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222"
        };

        Assert.True(options.IsProductionReady);
    }

    [Theory]
    [InlineData(null, "client-guid")]
    [InlineData("", "client-guid")]
    [InlineData("   ", "client-guid")]
    [InlineData("tenant-guid", null)]
    [InlineData("tenant-guid", "")]
    [InlineData("tenant-guid", "   ")]
    public void IsProductionReady_WhenAnyRequiredFieldMissing_ReturnsFalse(string? tenantId, string? clientId)
    {
        var options = new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = tenantId,
            ClientId = clientId
        };

        Assert.False(options.IsProductionReady);
    }

    [Theory]
    [InlineData("not-a-guid", "22222222-2222-2222-2222-222222222222")]
    [InlineData("11111111-1111-1111-1111-111111111111", "not-a-guid")]
    [InlineData("not-a-guid", "still-not-a-guid")]
    public void IsProductionReady_WhenAnyRequiredFieldIsNotGuid_ReturnsFalse(string tenantId, string clientId)
    {
        var options = new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = tenantId,
            ClientId = clientId
        };

        Assert.False(options.IsProductionReady);
    }

    [Fact]
    public void IsProductionReady_WhenDisabled_ReturnsFalse()
    {
        var options = new GraphConnectorOptions
        {
            Enabled = false,
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222"
        };

        Assert.False(options.IsProductionReady);
    }

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
