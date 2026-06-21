using Aura.Application.Models;
using Aura.UnitTests.Ingestion.Fakes;
using System.Globalization;

namespace Aura.UnitTests.Ingestion;

public class InMemoryCheckpointStoreContractTests
{
    [Fact]
    public async Task SaveAndGet_KeepsCheckpointsIndependent_PerDistinctIdentity()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var messagesIdentity = new CheckpointIdentity("teams", "messages", "acme");
        var calendarIdentity = new CheckpointIdentity("teams", "calendar", "acme");
        var messagesCheckpoint = new IngestionCheckpoint(
            "delta-messages",
            DateTimeOffset.Parse("2026-06-19T09:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T09:05:00Z", CultureInfo.InvariantCulture));
        var calendarCheckpoint = new IngestionCheckpoint(
            "delta-calendar",
            DateTimeOffset.Parse("2026-06-19T10:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T10:07:00Z", CultureInfo.InvariantCulture));

        await store.SaveAsync(messagesIdentity, messagesCheckpoint, CancellationToken.None);
        await store.SaveAsync(calendarIdentity, calendarCheckpoint, CancellationToken.None);

        var storedMessages = await store.GetAsync(messagesIdentity, CancellationToken.None);
        var storedCalendar = await store.GetAsync(calendarIdentity, CancellationToken.None);

        Assert.Equal(messagesCheckpoint, storedMessages);
        Assert.Equal(calendarCheckpoint, storedCalendar);
    }

    [Fact]
    public async Task Save_ReplacesCheckpoint_WhenIdentityMatches()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("github", "pull-requests", "acme");
        var original = new IngestionCheckpoint(
            "delta-v1",
            DateTimeOffset.Parse("2026-06-19T08:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T08:03:00Z", CultureInfo.InvariantCulture));
        var replacement = new IngestionCheckpoint(
            "delta-v2",
            DateTimeOffset.Parse("2026-06-19T11:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T11:04:00Z", CultureInfo.InvariantCulture));

        await store.SaveAsync(identity, original, CancellationToken.None);
        await store.SaveAsync(identity, replacement, CancellationToken.None);

        var stored = await store.GetAsync(identity, CancellationToken.None);

        Assert.Equal(replacement, stored);
        Assert.NotEqual(original, stored);
    }

    [Fact]
    public async Task Get_ReturnsStoredCheckpoint_WhenIdentityExists()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");
        var checkpoint = new IngestionCheckpoint(
            "delta-inbox",
            DateTimeOffset.Parse("2026-06-19T07:30:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T07:35:00Z", CultureInfo.InvariantCulture));

        await store.SaveAsync(identity, checkpoint, CancellationToken.None);

        var stored = await store.GetAsync(identity, CancellationToken.None);

        Assert.Equal(checkpoint, stored);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenIdentityDoesNotExist()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");

        var stored = await store.GetAsync(identity, CancellationToken.None);

        Assert.Null(stored);
    }

    [Fact]
    public async Task SaveAndGet_RoundTripFullValue_Unchanged()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var expected = new IngestionCheckpoint(
            "delta-abc",
            DateTimeOffset.Parse("2026-06-18T10:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-18T10:05:00Z", CultureInfo.InvariantCulture));

        await store.SaveAsync(identity, expected, CancellationToken.None);

        var actual = await store.GetAsync(identity, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SaveAndGet_PreservesNullFields()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var expected = new IngestionCheckpoint(
            null,
            DateTimeOffset.Parse("2026-06-18T08:00:00Z", CultureInfo.InvariantCulture),
            null);

        await store.SaveAsync(identity, expected, CancellationToken.None);

        var actual = await store.GetAsync(identity, CancellationToken.None);

        Assert.Equal(expected, actual);
        Assert.Null(actual!.Cursor);
        Assert.Equal(expected.MaxProcessedAt, actual.MaxProcessedAt);
        Assert.Null(actual.ExecutionFinishedAt);
    }

    [Fact]
    public async Task SaveAndGet_PreservesBothTimestampsNull_WhenOnlyCursorProvided()
    {
        var store = new InMemoryIngestionCheckpointStore();
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var expected = new IngestionCheckpoint("delta-v1", null, null);

        await store.SaveAsync(identity, expected, CancellationToken.None);

        var actual = await store.GetAsync(identity, CancellationToken.None);

        Assert.NotNull(actual);
        Assert.Equal("delta-v1", actual!.Cursor);
        Assert.Null(actual.MaxProcessedAt);
        Assert.Null(actual.ExecutionFinishedAt);
    }
}
