using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Demo;

namespace Aura.UnitTests.Demo;

public class QdrantFallbackHandlerTests
{
    [Fact]
    public async Task RetrieveAsync_ReturnsEmptyList()
    {
        var handler = new QdrantFallbackSemanticContextRetriever();

        var result = await handler.RetrieveAsync(
            new SemanticQuery { Text = "test query", TopK = 10 },
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task WriteAsync_DoesNotThrow()
    {
        var handler = new QdrantFallbackSemanticIndexWriter();

        await handler.WriteAsync(
            new List<EmbeddedSemanticChunk>().AsReadOnly(),
            CancellationToken.None);
    }

    [Fact]
    public async Task DeleteByCanonicalIdAsync_DoesNotThrow()
    {
        var handler = new QdrantFallbackSemanticIndexWriter();

        await handler.DeleteByCanonicalIdAsync("some-id", CancellationToken.None);
    }

    [Fact]
    public void FallbackRetriever_ImplementsISemanticContextRetriever()
    {
        var handler = new QdrantFallbackSemanticContextRetriever();
        Assert.IsAssignableFrom<ISemanticContextRetriever>(handler);
    }

    [Fact]
    public void FallbackWriter_ImplementsISemanticIndexWriter()
    {
        var handler = new QdrantFallbackSemanticIndexWriter();
        Assert.IsAssignableFrom<ISemanticIndexWriter>(handler);
    }
}
