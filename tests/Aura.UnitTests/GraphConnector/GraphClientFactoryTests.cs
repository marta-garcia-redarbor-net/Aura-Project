using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.GraphConnector;

public class GraphClientFactoryTests
{
    private readonly IConfidentialClientApplication _msalApp;
    private readonly IOptions<GraphConnectorOptions> _options;
    private readonly GraphClientFactory _factory;

    public GraphClientFactoryTests()
    {
        _msalApp = Substitute.For<IConfidentialClientApplication>();
        _options = Options.Create(new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = "test-tenant",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            Scopes = ["Mail.Read", "Chat.Read"]
        });
        _factory = new GraphClientFactory(_msalApp, _options);
    }

    [Fact]
    public async Task CreateClientAsync_NoAccount_ThrowsMsalUiRequiredException()
    {
        // Arrange: no cached accounts — simulates fresh install or cleared cache
#pragma warning disable CS0618 // GetAccountsAsync is obsolete in newer MSAL but used here
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
#pragma warning restore CS0618

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => _factory.CreateClientAsync(CancellationToken.None));

        Assert.Equal("no_account", ex.ErrorCode);
        Assert.Contains("No cached account", ex.Message);
    }

    [Fact]
    public async Task CreateClientAsync_ExpiredToken_PropagatesMsalUiRequiredException()
    {
        // Arrange: account exists but AcquireTokenSilent fails (expired/invalid token)
        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.Username.Returns("user@contoso.com");

#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        // MSAL throws MsalUiRequiredException when silent acquisition fails
        _msalApp.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), fakeAccount)
            .Throws(new MsalUiRequiredException("interaction_required",
                "Token has expired. User must re-authenticate."));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => _factory.CreateClientAsync(CancellationToken.None));

        Assert.Equal("interaction_required", ex.ErrorCode);
    }

    [Fact]
    public void Constructor_NullMsalApp_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientFactory(null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientFactory(_msalApp, null!));
    }

    [Fact]
    public void Constructor_WithNullScopes_UsesDefaults()
    {
        // Arrange: options without scopes should not throw
        var optionsNoScopes = Options.Create(new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = "test-tenant",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            Scopes = null
        });

        // Act — should not throw
        var factory = new GraphClientFactory(_msalApp, optionsNoScopes);

        // Assert: factory was created successfully (scopes resolved to defaults internally)
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task CreateClientAsync_WithAccount_CallsAcquireTokenSilentWithConfiguredScopes()
    {
        // Arrange: account exists — verify scopes are passed correctly
        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.Username.Returns("user@contoso.com");

#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        // AcquireTokenSilent will be called — capture the scopes argument
        // Even though we can't mock ExecuteAsync on the builder,
        // we verify the method is called with correct scopes by checking what throws
        IEnumerable<string>? capturedScopes = null;
        _msalApp.AcquireTokenSilent(
                Arg.Do<IEnumerable<string>>(s => capturedScopes = s),
                Arg.Any<IAccount>())
            .Throws(new MsalUiRequiredException("test", "test"));

        // Act
        try
        {
            await _factory.CreateClientAsync(CancellationToken.None);
        }
        catch (MsalUiRequiredException)
        {
            // Expected — we're testing the scopes argument, not the result
        }

        // Assert: configured scopes were passed to MSAL
        Assert.NotNull(capturedScopes);
        var scopeArray = capturedScopes.ToArray();
        Assert.Contains("Mail.Read", scopeArray);
        Assert.Contains("Chat.Read", scopeArray);
    }

    [Fact]
    public async Task CreateClientAsync_WithNullScopes_PassesDefaultScopes()
    {
        // Arrange: factory with null scopes option
        var optionsNoScopes = Options.Create(new GraphConnectorOptions
        {
            Enabled = true,
            ClientId = "test-client",
            ClientSecret = "test-secret",
            Scopes = null
        });
        var factory = new GraphClientFactory(_msalApp, optionsNoScopes);

        var fakeAccount = Substitute.For<IAccount>();
#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        IEnumerable<string>? capturedScopes = null;
        _msalApp.AcquireTokenSilent(
                Arg.Do<IEnumerable<string>>(s => capturedScopes = s),
                Arg.Any<IAccount>())
            .Throws(new MsalUiRequiredException("test", "test"));

        // Act
        try
        {
            await factory.CreateClientAsync(CancellationToken.None);
        }
        catch (MsalUiRequiredException)
        {
            // Expected
        }

        // Assert: default scopes were passed
        Assert.NotNull(capturedScopes);
        var scopeArray = capturedScopes.ToArray();
        Assert.Contains("Mail.Read", scopeArray);
        Assert.Contains("Chat.Read", scopeArray);
        Assert.Contains("User.Read", scopeArray);
    }
}
