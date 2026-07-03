using Aura.Infrastructure.Adapters.Rules;
using Microsoft.Data.Sqlite;

namespace Aura.IntegrationTests.Stores;

public sealed class SqliteAlertRuleStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteAlertRuleStore _store;

    public SqliteAlertRuleStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteAlertRuleStore.InitializeSchema(_connection);
        _store = new SqliteAlertRuleStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task AddAndGetVipSenders_ReturnsAddedSenders()
    {
        await _store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        await _store.AddVipSenderAsync("vip@company.com", "admin", CancellationToken.None);

        var senders = await _store.GetVipSendersAsync(CancellationToken.None);

        Assert.Equal(2, senders.Count);
        Assert.Contains("boss@company.com", senders);
        Assert.Contains("vip@company.com", senders);
    }

    [Fact]
    public async Task RemoveVipSender_NoLongerReturnsIt()
    {
        await _store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        await _store.RemoveVipSenderAsync("boss@company.com", CancellationToken.None);

        var senders = await _store.GetVipSendersAsync(CancellationToken.None);

        Assert.Empty(senders);
    }

    [Fact]
    public async Task AddAndGetKeywords_ReturnsAddedKeywords()
    {
        await _store.AddKeywordAsync("urgent", "admin", CancellationToken.None);
        await _store.AddKeywordAsync("critical", "admin", CancellationToken.None);

        var keywords = await _store.GetKeywordsAsync(CancellationToken.None);

        Assert.Equal(2, keywords.Count);
        Assert.Contains("urgent", keywords);
        Assert.Contains("critical", keywords);
    }

    [Fact]
    public async Task RemoveKeyword_NoLongerReturnsIt()
    {
        await _store.AddKeywordAsync("urgent", "admin", CancellationToken.None);
        await _store.RemoveKeywordAsync("urgent", CancellationToken.None);

        var keywords = await _store.GetKeywordsAsync(CancellationToken.None);

        Assert.Empty(keywords);
    }
}
