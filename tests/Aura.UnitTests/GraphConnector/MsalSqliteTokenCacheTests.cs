using Aura.Infrastructure.Adapters.Connectors.Graph;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.GraphConnector;

public class MsalSqliteTokenCacheTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MsalSqliteTokenCache _cache;

    public MsalSqliteTokenCacheTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        MsalSqliteTokenCache.InitializeSchema(_connection);
        _cache = new MsalSqliteTokenCache(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void PersistAndRetrieve_RoundTrips()
    {
        var cacheKey = "user-1";
        var data = new byte[] { 0x01, 0x02, 0x03, 0xAA, 0xBB };

        _cache.Persist(cacheKey, data);
        var retrieved = _cache.Retrieve(cacheKey);

        Assert.NotNull(retrieved);
        Assert.Equal(data, retrieved);
    }

    [Fact]
    public void Retrieve_NoData_ReturnsNull()
    {
        var retrieved = _cache.Retrieve("nonexistent");

        Assert.Null(retrieved);
    }

    [Fact]
    public void Persist_OverwritesExistingData()
    {
        var cacheKey = "user-overwrite";
        var data1 = new byte[] { 0x01 };
        var data2 = new byte[] { 0x02, 0x03 };

        _cache.Persist(cacheKey, data1);
        _cache.Persist(cacheKey, data2);
        var retrieved = _cache.Retrieve(cacheKey);

        Assert.NotNull(retrieved);
        Assert.Equal(data2, retrieved);
    }

    [Fact]
    public void MultipleKeys_IndependentStorage()
    {
        var data1 = new byte[] { 0xAA };
        var data2 = new byte[] { 0xBB };

        _cache.Persist("key-1", data1);
        _cache.Persist("key-2", data2);

        Assert.Equal(data1, _cache.Retrieve("key-1"));
        Assert.Equal(data2, _cache.Retrieve("key-2"));
    }

    [Fact]
    public void Persist_DemoAndRealKeys_StayIsolated()
    {
        var realData = new byte[] { 0x10, 0x20 };
        var demoData = new byte[] { 0x30, 0x40 };

        _cache.Persist("msal-user-real-oid-A", realData);
        _cache.Persist("demo-session-001", demoData);

        Assert.Equal(realData, _cache.Retrieve("msal-user-real-oid-A"));
        Assert.Equal(demoData, _cache.Retrieve("demo-session-001"));
        Assert.Null(_cache.Retrieve("msal-user-real-oid-B"));
    }

    [Fact]
    public void HasCachedData_WithData_ReturnsTrue()
    {
        _cache.Persist("key-check", new byte[] { 0x01 });

        Assert.True(_cache.HasCachedData("key-check"));
    }

    [Fact]
    public void HasCachedData_NoData_ReturnsFalse()
    {
        Assert.False(_cache.HasCachedData("no-key"));
    }

    [Fact]
    public void Persist_OidPatternKeys_IsolatePerOid()
    {
        // Cache keys follow the msal-user-{oid} pattern used by DependencyInjection.cs
        // when MSAL provides a per-user SuggestedCacheKey.
        var dataA = new byte[] { 0xAA };
        var dataB = new byte[] { 0xBB };

        _cache.Persist("msal-user-oid-A", dataA);
        _cache.Persist("msal-user-oid-B", dataB);

        // Each oid retrieves only its own data
        Assert.Equal(dataA, _cache.Retrieve("msal-user-oid-A"));
        Assert.Equal(dataB, _cache.Retrieve("msal-user-oid-B"));

        // Unknown oid returns null — no cross-contamination (previous bug: all shared same key)
        Assert.Null(_cache.Retrieve("msal-user-oid-unknown"));
    }

    [Fact]
    public void Persist_FallbackKeyFormat_StoresUnderClientIdUnknown()
    {
        // The fallback key format msal-user-{ClientId}-unknown is used when
        // MSAL does not provide a SuggestedCacheKey.
        var data = new byte[] { 0xFF };

        _cache.Persist("msal-user-test-client-unknown", data);

        Assert.True(_cache.HasCachedData("msal-user-test-client-unknown"));
        Assert.Equal(data, _cache.Retrieve("msal-user-test-client-unknown"));
    }
}
