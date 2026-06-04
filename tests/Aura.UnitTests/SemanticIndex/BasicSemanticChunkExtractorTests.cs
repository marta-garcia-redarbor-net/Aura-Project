using Aura.Application.Services;
using Aura.Domain.SemanticIndex.Enums;

namespace Aura.UnitTests.SemanticIndex;

public class BasicSemanticChunkExtractorTests
{
    private readonly BasicSemanticChunkExtractor _extractor = new();

    // ── Chunking a large source event ──────────────────────────────────

    [Fact]
    public async Task ExtractAsync_ShortContent_ReturnsSingleChunk()
    {
        var chunks = await _extractor.ExtractAsync(
            "src-001", "Short content.", SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        Assert.Single(chunks);
        Assert.Equal("src-001", chunks[0].CanonicalSourceId);
        Assert.Equal("Short content.", chunks[0].Content);
        Assert.Equal(SemanticCollectionType.ProjectKnowledge, chunks[0].Collection);
    }

    [Fact]
    public async Task ExtractAsync_LargeContent_SplitsIntoMultipleChunks()
    {
        // Create content larger than chunk size (>1000 chars)
        var paragraph1 = new string('A', 600);
        var paragraph2 = new string('B', 600);
        var largeContent = $"{paragraph1}\n\n{paragraph2}";

        var chunks = await _extractor.ExtractAsync(
            "src-002", largeContent, SemanticCollectionType.ActivityMemory, CancellationToken.None);

        Assert.True(chunks.Count >= 2, $"Expected at least 2 chunks but got {chunks.Count}");
        Assert.All(chunks, c => Assert.Equal("src-002", c.CanonicalSourceId));
        Assert.All(chunks, c => Assert.Equal(SemanticCollectionType.ActivityMemory, c.Collection));
        Assert.All(chunks, c => Assert.NotEmpty(c.Content));
    }

    [Fact]
    public async Task ExtractAsync_EachChunkHasUniqueId()
    {
        var content = string.Join("\n\n", Enumerable.Range(0, 3).Select(i => new string((char)('A' + i), 600)));

        var chunks = await _extractor.ExtractAsync(
            "src-003", content, SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        var ids = chunks.Select(c => c.Id).ToHashSet();
        Assert.Equal(chunks.Count, ids.Count);
    }

    // ── Handling sensitive content (PII stripping) ─────────────────────

    [Fact]
    public async Task ExtractAsync_ContentWithEmail_StripsPii()
    {
        var content = "Contact john.doe@example.com for details.";

        var chunks = await _extractor.ExtractAsync(
            "src-pii-1", content, SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        var resultContent = chunks[0].Content;
        Assert.DoesNotContain("john.doe@example.com", resultContent);
        Assert.Contains("[REDACTED]", resultContent);
    }

    [Fact]
    public async Task ExtractAsync_ContentWithSsn_StripsPii()
    {
        var content = "Employee SSN is 123-45-6789 on file.";

        var chunks = await _extractor.ExtractAsync(
            "src-pii-2", content, SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        var resultContent = chunks[0].Content;
        Assert.DoesNotContain("123-45-6789", resultContent);
        Assert.Contains("[REDACTED]", resultContent);
    }

    [Fact]
    public async Task ExtractAsync_ContentWithMultiplePiiTypes_StripsAll()
    {
        var content = "User jane@corp.io has SSN 987-65-4321 and phone +1-555-123-4567.";

        var chunks = await _extractor.ExtractAsync(
            "src-pii-3", content, SemanticCollectionType.ActivityMemory, CancellationToken.None);

        var resultContent = chunks[0].Content;
        Assert.DoesNotContain("jane@corp.io", resultContent);
        Assert.DoesNotContain("987-65-4321", resultContent);
    }

    [Fact]
    public async Task ExtractAsync_ContentWithoutPii_PreservesContent()
    {
        var content = "Clean technical documentation about patterns.";

        var chunks = await _extractor.ExtractAsync(
            "src-clean", content, SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        Assert.Equal("Clean technical documentation about patterns.", chunks[0].Content);
    }

    // ── Basic tagging ──────────────────────────────────────────────────

    [Fact]
    public async Task ExtractAsync_AssignsCollectionTag()
    {
        var chunks = await _extractor.ExtractAsync(
            "src-tag-1", "Some content", SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        Assert.Contains(chunks[0].Tags, t => t.Key == "collection" && t.Value == "ProjectKnowledge");
    }

    [Fact]
    public async Task ExtractAsync_ActivityMemory_AssignsCorrectCollectionTag()
    {
        var chunks = await _extractor.ExtractAsync(
            "src-tag-2", "Activity content", SemanticCollectionType.ActivityMemory, CancellationToken.None);

        Assert.Contains(chunks[0].Tags, t => t.Key == "collection" && t.Value == "ActivityMemory");
    }

    // ── Input validation ───────────────────────────────────────────────

    [Fact]
    public async Task ExtractAsync_NullCanonicalSourceId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _extractor.ExtractAsync(null!, "content", SemanticCollectionType.ProjectKnowledge, CancellationToken.None));
    }

    [Fact]
    public async Task ExtractAsync_EmptyContent_ReturnsEmpty()
    {
        var chunks = await _extractor.ExtractAsync(
            "src-empty", "", SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        Assert.Empty(chunks);
    }
}
