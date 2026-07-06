using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Services;

namespace Aura.UnitTests.Infrastructure;

public class InMemoryErrorStoreTests
{
    [Fact]
    public async Task RecordAsync_Then_GetRecentAsync_ReturnsRecordedEntries()
    {
        var store = new InMemoryErrorStore();
        var entry = new ErrorEntry("corr-1", DateTimeOffset.UtcNow, "error message");

        await store.RecordAsync(entry, CancellationToken.None);
        var result = await store.GetRecentAsync(10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("corr-1", result[0].CorrelationId);
        Assert.Equal("error message", result[0].Message);
    }

    [Fact]
    public async Task GetRecentAsync_WhenEmpty_ReturnsEmptyList()
    {
        var store = new InMemoryErrorStore();

        var result = await store.GetRecentAsync(10, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RecordAsync_RespectsCapacity_ReturnsMostRecent()
    {
        var store = new InMemoryErrorStore(capacity: 3);

        await store.RecordAsync(new ErrorEntry("1", DateTimeOffset.UtcNow, "first"), CancellationToken.None);
        await store.RecordAsync(new ErrorEntry("2", DateTimeOffset.UtcNow, "second"), CancellationToken.None);
        await store.RecordAsync(new ErrorEntry("3", DateTimeOffset.UtcNow, "third"), CancellationToken.None);
        await store.RecordAsync(new ErrorEntry("4", DateTimeOffset.UtcNow, "fourth"), CancellationToken.None);

        var result = await store.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("4", result[0].CorrelationId);
        Assert.Equal("3", result[1].CorrelationId);
        Assert.Equal("2", result[2].CorrelationId);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsUpToRequestedCount()
    {
        var store = new InMemoryErrorStore();

        for (int i = 0; i < 10; i++)
        {
            await store.RecordAsync(new ErrorEntry(i.ToString(), DateTimeOffset.UtcNow, $"err {i}"), CancellationToken.None);
        }

        var result = await store.GetRecentAsync(3, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task RecordAsync_IsThreadSafe_NoException()
    {
        var store = new InMemoryErrorStore(capacity: 100);
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(() => store.RecordAsync(
                new ErrorEntry(idx.ToString(), DateTimeOffset.UtcNow, $"err {idx}"),
                CancellationToken.None)));
        }

        await Task.WhenAll(tasks);

        var result = await store.GetRecentAsync(100, CancellationToken.None);
        Assert.Equal(50, result.Count);
    }
}
