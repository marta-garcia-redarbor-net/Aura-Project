using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Infrastructure.Adapters.SemanticOutbox;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.Persistence;

public class SqliteSemanticOutboxRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISemanticOutboxRepository _repository;

    public SqliteSemanticOutboxRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteSemanticOutboxRepository.InitializeSchema(_connection);
        _repository = new SqliteSemanticOutboxRepository(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task EnqueueAndFetch_ReturnsEnqueuedEntry()
    {
        var entry = CreateEntry("src-001", "Test content", SemanticCollectionType.ProjectKnowledge);
        await _repository.EnqueueAsync(entry, CancellationToken.None);

        var pending = await _repository.FetchPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal(entry.Id, pending[0].Id);
        Assert.Equal("src-001", pending[0].CanonicalSourceId);
        Assert.Equal("Test content", pending[0].Content);
        Assert.Equal(SemanticCollectionType.ProjectKnowledge, pending[0].Collection);
        Assert.False(pending[0].Processed);
    }

    [Fact]
    public async Task FetchPending_RespectsProcessedFlag()
    {
        var entry1 = CreateEntry("src-001", "content1", SemanticCollectionType.ProjectKnowledge);
        var entry2 = CreateEntry("src-002", "content2", SemanticCollectionType.ActivityMemory);
        await _repository.EnqueueAsync(entry1, CancellationToken.None);
        await _repository.EnqueueAsync(entry2, CancellationToken.None);

        entry1.MarkProcessed();
        await _repository.UpdateAsync(entry1, CancellationToken.None);

        var pending = await _repository.FetchPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal(entry2.Id, pending[0].Id);
    }

    [Fact]
    public async Task FetchPending_RespectsMaxBatchSize()
    {
        for (var i = 0; i < 5; i++)
        {
            await _repository.EnqueueAsync(
                CreateEntry($"src-{i}", $"content-{i}", SemanticCollectionType.ProjectKnowledge),
                CancellationToken.None);
        }

        var pending = await _repository.FetchPendingAsync(3, CancellationToken.None);

        Assert.Equal(3, pending.Count);
    }

    [Fact]
    public async Task FetchPending_OrdersByCreatedAt()
    {
        var older = new SemanticOutboxEntry(
            Guid.NewGuid(), "src-old", "old content",
            SemanticCollectionType.ProjectKnowledge,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = new SemanticOutboxEntry(
            Guid.NewGuid(), "src-new", "new content",
            SemanticCollectionType.ActivityMemory,
            DateTimeOffset.UtcNow);

        await _repository.EnqueueAsync(newer, CancellationToken.None);
        await _repository.EnqueueAsync(older, CancellationToken.None);

        var pending = await _repository.FetchPendingAsync(10, CancellationToken.None);

        Assert.Equal(2, pending.Count);
        Assert.Equal("src-old", pending[0].CanonicalSourceId);
        Assert.Equal("src-new", pending[1].CanonicalSourceId);
    }

    [Fact]
    public async Task Update_PersistsErrorState()
    {
        var entry = CreateEntry("src-fail", "content", SemanticCollectionType.ActivityMemory);
        await _repository.EnqueueAsync(entry, CancellationToken.None);

        entry.MarkFailed("Embedding timeout");
        await _repository.UpdateAsync(entry, CancellationToken.None);

        var pending = await _repository.FetchPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal("Embedding timeout", pending[0].Error);
        Assert.False(pending[0].Processed);
    }

    [Fact]
    public async Task FetchPending_EmptyOutbox_ReturnsEmptyList()
    {
        var pending = await _repository.FetchPendingAsync(10, CancellationToken.None);

        Assert.Empty(pending);
    }

    private static SemanticOutboxEntry CreateEntry(
        string canonicalSourceId, string content, SemanticCollectionType collection)
    {
        return new SemanticOutboxEntry(
            Guid.NewGuid(), canonicalSourceId, content, collection, DateTimeOffset.UtcNow);
    }
}
