using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.GraphConnector;

public class GraphClientFactoryTests
{
    private readonly IPublicClientApplication _msalApp;
    private readonly SqliteConnection _connection;
    private readonly UserTokenStore _userTokenStore;
    private readonly IOptions<GraphConnectorOptions> _options;
    private readonly GraphClientFactory _factory;

    public GraphClientFactoryTests()
    {
        _msalApp = Substitute.For<IPublicClientApplication>();
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        UserTokenStore.InitializeSchema(_connection);
        _userTokenStore = new UserTokenStore(_connection);
        _options = Options.Create(new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = "test-tenant",
            ClientId = "test-client",
            Scopes = ["Mail.Read", "Chat.Read"]
        });
        _factory = new GraphClientFactory(_msalApp, _userTokenStore, _options);
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
            () => _factory.CreateClientAsync("oid-test", CancellationToken.None));

        Assert.Equal("no_account", ex.ErrorCode);
        Assert.Contains("No cached account", ex.Message);
    }

    [Fact]
    public async Task CreateClientAsync_ExpiredToken_PropagatesMsalUiRequiredException()
    {
        // Arrange: account exists but AcquireTokenSilent fails (expired/invalid token)
        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));

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
            () => _factory.CreateClientAsync("oid-A", CancellationToken.None));

        Assert.Equal("interaction_required", ex.ErrorCode);
    }

    [Fact]
    public void Constructor_NullMsalApp_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientFactory(null!, _userTokenStore, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientFactory(_msalApp, _userTokenStore, null!));
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
            Scopes = null
        });

        // Act — should not throw
        var factory = new GraphClientFactory(_msalApp, _userTokenStore, optionsNoScopes);

        // Assert: factory was created successfully (scopes resolved to defaults internally)
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task CreateClientAsync_WithAccount_CallsAcquireTokenSilentWithConfiguredScopes()
    {
        // Arrange: account exists — verify scopes are passed correctly
        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));

#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        // AcquireTokenSilent will be called — capture the scopes argument
        IEnumerable<string>? capturedScopes = null;
        _msalApp.AcquireTokenSilent(
                Arg.Do<IEnumerable<string>>(s => capturedScopes = s),
                Arg.Any<IAccount>())
            .Throws(new MsalUiRequiredException("test", "test"));

        // Act
        try
        {
            await _factory.CreateClientAsync("oid-A", CancellationToken.None);
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
            Scopes = null
        });
        var factory = new GraphClientFactory(_msalApp, _userTokenStore, optionsNoScopes);

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));
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
            await factory.CreateClientAsync("oid-A", CancellationToken.None);
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
        Assert.Contains("Calendars.Read", scopeArray);
    }

    [Fact]
    public async Task CreateClientAsync_OidMatch_SelectsCorrectAccount()
    {
        // Arrange: two accounts with different oids
        var accountA = Substitute.For<IAccount>();
        accountA.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));
        accountA.Username.Returns("alice@contoso.com");

        var accountB = Substitute.For<IAccount>();
        accountB.HomeAccountId.Returns(new AccountId("oid-B", "oid-B", null));
        accountB.Username.Returns("bob@contoso.com");

#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[accountA, accountB]));
#pragma warning restore CS0618

        IAccount? capturedAccount = null;
        _msalApp.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>())
            .Returns(ci =>
            {
                capturedAccount = ci.ArgAt<IAccount>(1);
                throw new MsalUiRequiredException("test", "test");
            });

        // Act
        try
        {
            await _factory.CreateClientAsync("oid-B", CancellationToken.None);
        }
        catch (MsalUiRequiredException)
        {
            // Expected
        }

        // Assert: correct account was selected
        Assert.NotNull(capturedAccount);
        Assert.Equal("oid-B", capturedAccount!.HomeAccountId.ObjectId);
    }

    [Fact]
    public async Task CreateClientAsync_NoMatchingOid_ThrowsMsalUiRequiredException()
    {
        // Arrange: one account with oid-A, request with oid-C
        var accountA = Substitute.For<IAccount>();
        accountA.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));

#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[accountA]));
#pragma warning restore CS0618

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => _factory.CreateClientAsync("oid-C", CancellationToken.None));

        Assert.Equal("no_account", ex.ErrorCode);

#pragma warning disable CS0618
        _msalApp.DidNotReceive().AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>());
#pragma warning restore CS0618
    }

    [Fact]
    public async Task CreateClientAsync_EmptyCache_ThrowsMsalUiRequiredException()
    {
        // Arrange: no accounts at all
#pragma warning disable CS0618
        _msalApp.GetAccountsAsync()
            .Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
#pragma warning restore CS0618

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MsalUiRequiredException>(
            () => _factory.CreateClientAsync("oid-unknown", CancellationToken.None));

        Assert.Equal("no_account", ex.ErrorCode);
    }

    [Fact]
    public async Task DefaultScopes_IncludeCalendarsRead()
    {
        // Arrange: factory created with null scopes (uses defaults)
        var optionsNoScopes = Options.Create(new GraphConnectorOptions
        {
            Enabled = true,
            TenantId = "test-tenant",
            ClientId = "test-client",
            Scopes = null
        });
        var factory = new GraphClientFactory(_msalApp, _userTokenStore, optionsNoScopes);

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-A", "oid-A", null));
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
            await factory.CreateClientAsync("oid-A", CancellationToken.None);
        }
        catch (MsalUiRequiredException)
        {
            // Expected — we're testing the scopes argument
        }

        // Assert: Calendars.Read is present in default scopes
        Assert.NotNull(capturedScopes);
        var scopeArray = capturedScopes.ToArray();
        Assert.Contains("Calendars.Read", scopeArray);
        Assert.Contains("Mail.Read", scopeArray);
        Assert.Contains("Chat.Read", scopeArray);
        Assert.Contains("User.Read", scopeArray);
    }
}
