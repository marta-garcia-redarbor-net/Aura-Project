using Aura.Application.Models;
using Aura.Application.UseCases.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Aura.UnitTests.MorningSummary;

public class SqliteMorningSummaryEmissionStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteMorningSummaryEmissionStore _store;

    public SqliteMorningSummaryEmissionStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteMorningSummaryEmissionStore.InitializeSchema(_connection);
        _store = new SqliteMorningSummaryEmissionStore(_connection);
    }

    [Fact]
    public async Task HasBeenEmittedAsync_AfterMark_ReturnsTrue()
    {
        var userId = "system";
        var localDate = new DateOnly(2026, 6, 23);

        await _store.MarkEmittedAsync(userId, localDate, CancellationToken.None);

        var emitted = await _store.HasBeenEmittedAsync(userId, localDate, CancellationToken.None);
        Assert.True(emitted);
    }

    [Fact]
    public async Task MarkEmittedAsync_SameDayDuplicate_DoesNotThrowAndStaysEmitted()
    {
        var userId = "system";
        var localDate = new DateOnly(2026, 6, 23);

        await _store.MarkEmittedAsync(userId, localDate, CancellationToken.None);
        await _store.MarkEmittedAsync(userId, localDate, CancellationToken.None);

        var emitted = await _store.HasBeenEmittedAsync(userId, localDate, CancellationToken.None);
        Assert.True(emitted);
    }

    [Fact]
    public async Task HasBeenEmittedAsync_NextDay_IsIndependent()
    {
        var userId = "system";
        var dayOne = new DateOnly(2026, 6, 23);
        var dayTwo = dayOne.AddDays(1);

        await _store.MarkEmittedAsync(userId, dayOne, CancellationToken.None);

        var emittedDayOne = await _store.HasBeenEmittedAsync(userId, dayOne, CancellationToken.None);
        var emittedDayTwo = await _store.HasBeenEmittedAsync(userId, dayTwo, CancellationToken.None);

        Assert.True(emittedDayOne);
        Assert.False(emittedDayTwo);
    }

    [Fact]
    public async Task ResetAsync_RemovesEmissionGuardForDate()
    {
        var userId = "system";
        var localDate = new DateOnly(2026, 6, 23);

        await _store.MarkEmittedAsync(userId, localDate, CancellationToken.None);
        await _store.ResetAsync(userId, localDate, CancellationToken.None);

        var emitted = await _store.HasBeenEmittedAsync(userId, localDate, CancellationToken.None);
        Assert.False(emitted);
    }

    [Fact]
    public async Task HasBeenEmittedAsync_PersistsAcrossRepositoryRestart_OnSameDatabase()
    {
        var userId = "system";
        var localDate = new DateOnly(2026, 6, 23);

        var dbFilePath = Path.Combine(Path.GetTempPath(), $"aura-ms-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFilePath};Pooling=False";

        try
        {
            using (var firstConnection = new SqliteConnection(connectionString))
            {
                firstConnection.Open();
                SqliteMorningSummaryEmissionStore.InitializeSchema(firstConnection);

                var firstStore = new SqliteMorningSummaryEmissionStore(firstConnection);
                await firstStore.MarkEmittedAsync(userId, localDate, CancellationToken.None);
            }

            using (var secondConnection = new SqliteConnection(connectionString))
            {
                secondConnection.Open();
                SqliteMorningSummaryEmissionStore.InitializeSchema(secondConnection);

                var reopenedStore = new SqliteMorningSummaryEmissionStore(secondConnection);
                var emitted = await reopenedStore.HasBeenEmittedAsync(userId, localDate, CancellationToken.None);

                Assert.True(emitted);
            }
        }
        finally
        {
            if (File.Exists(dbFilePath))
            {
                File.Delete(dbFilePath);
            }
        }
    }

    [Fact]
    public async Task Scheduler_WithPersistedEmission_NextDayAtOrAfterTarget_IsDueAgain()
    {
        var userId = "system";
        var targetLocalTime = new TimeOnly(9, 0);
        var dayOne = new DateOnly(2026, 6, 23);

        await _store.MarkEmittedAsync(userId, dayOne, CancellationToken.None);

        var settingsProvider = new TestSettingsProvider(new MorningSummarySettings("UTC", targetLocalTime));
        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            _store,
            utcNow: () => DateTimeOffset.Parse("2026-06-24T09:00:00Z", CultureInfo.InvariantCulture));

        var due = await scheduler.ResolveAsync(userId, CancellationToken.None);

        Assert.True(due.IsDue);
        Assert.Equal(new DateOnly(2026, 6, 24), due.LocalDate);
        Assert.Equal(targetLocalTime, due.TargetLocalTime);
    }

    [Fact]
    public async Task Scheduler_AfterResetForSameDay_AllowsForcedReEmission()
    {
        var userId = "system";
        var targetLocalTime = new TimeOnly(9, 0);
        var localDate = new DateOnly(2026, 6, 23);

        await _store.MarkEmittedAsync(userId, localDate, CancellationToken.None);

        var settingsProvider = new TestSettingsProvider(new MorningSummarySettings("UTC", targetLocalTime));
        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            _store,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T09:30:00Z", CultureInfo.InvariantCulture));

        var blockedBeforeReset = await scheduler.ResolveAsync(userId, CancellationToken.None);
        Assert.False(blockedBeforeReset.IsDue);

        await _store.ResetAsync(userId, localDate, CancellationToken.None);

        var dueAfterReset = await scheduler.ResolveAsync(userId, CancellationToken.None);

        Assert.True(dueAfterReset.IsDue);
        Assert.Equal(localDate, dueAfterReset.LocalDate);
    }

    private sealed class TestSettingsProvider : Aura.Application.Ports.IMorningSummarySettingsProvider
    {
        private readonly MorningSummarySettings _settings;

        public TestSettingsProvider(MorningSummarySettings settings)
        {
            _settings = settings;
        }

        public MorningSummarySettings GetSettings() => _settings;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _connection.Dispose();
    }
}
