using System.IO;
using Aura.Domain.FocusState;
using Aura.Infrastructure.Adapters.FocusState;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.Infrastructure;

public class SqliteFocusStateOverrideStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteFocusStateOverrideStore _store;

    public SqliteFocusStateOverrideStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteFocusStateOverrideStore.InitializeSchema(_connection);
        _store = new SqliteFocusStateOverrideStore(_connection);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    [Fact]
    public async Task GetAsync_WhenNoOverride_ReturnsNull()
    {
        var result = await _store.GetAsync("user-nonexistent", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSetState()
    {
        await _store.SetAsync("user-1", FocusStateType.DeepWork);

        var result = await _store.GetAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.DeepWork, result);
    }

    [Fact]
    public async Task SetAsync_OverridesPreviousValue()
    {
        await _store.SetAsync("user-1", FocusStateType.DeepWork);
        await _store.SetAsync("user-1", FocusStateType.WindowOfOpportunity);

        var result = await _store.GetAsync("user-1", CancellationToken.None);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result);
    }

    [Fact]
    public async Task ClearAsync_RemovesOverride()
    {
        await _store.SetAsync("user-1", FocusStateType.DeepWork);
        await _store.ClearAsync("user-1");

        var result = await _store.GetAsync("user-1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_WhenNoOverride_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
            _store.ClearAsync("user-nonexistent"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task MultipleUsers_DoNotInterfere()
    {
        await _store.SetAsync("user-a", FocusStateType.DeepWork);
        await _store.SetAsync("user-b", FocusStateType.Away);

        var resultA = await _store.GetAsync("user-a", CancellationToken.None);
        var resultB = await _store.GetAsync("user-b", CancellationToken.None);

        Assert.Equal(FocusStateType.DeepWork, resultA);
        Assert.Equal(FocusStateType.Away, resultB);
    }

    [Fact]
    public async Task Override_PersistsAcrossStoreRecreation_OnSameDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-focus-{Guid.NewGuid():N}.db");

        try
        {
            await using (var firstConnection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await firstConnection.OpenAsync();
                SqliteFocusStateOverrideStore.InitializeSchema(firstConnection);
                var firstStore = new SqliteFocusStateOverrideStore(firstConnection);
                await firstStore.SetAsync("user-restart", FocusStateType.DeepWork);
            }

            await using (var secondConnection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await secondConnection.OpenAsync();
                SqliteFocusStateOverrideStore.InitializeSchema(secondConnection);
                var secondStore = new SqliteFocusStateOverrideStore(secondConnection);

                var persisted = await secondStore.GetAsync("user-restart", CancellationToken.None);

                Assert.Equal(FocusStateType.DeepWork, persisted);
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup for temp file used in persistence-boundary test.
                }
            }
        }
    }
}
