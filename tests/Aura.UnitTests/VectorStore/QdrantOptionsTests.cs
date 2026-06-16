using Aura.Domain.SemanticIndex.Enums;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

namespace Aura.UnitTests.VectorStore;

public class QdrantOptionsTests
{
    [Fact]
    public void DefaultHost_IsLocalhost()
    {
        var options = new QdrantOptions();
        Assert.Equal("localhost", options.Host);
    }

    [Fact]
    public void DefaultGrpcPort_Is6334()
    {
        var options = new QdrantOptions();
        Assert.Equal(6334, options.GrpcPort);
    }

    [Fact]
    public void DefaultApiKey_IsNull()
    {
        var options = new QdrantOptions();
        Assert.Null(options.ApiKey);
    }

    [Fact]
    public void DefaultVectorSize_Is1536()
    {
        var options = new QdrantOptions();
        Assert.Equal(1536, options.VectorSize);
    }

    [Fact]
    public void GetCollectionName_ProjectKnowledge_ReturnsExpected()
    {
        var options = new QdrantOptions();
        Assert.Equal("aura_project_knowledge", options.GetCollectionName(SemanticCollectionType.ProjectKnowledge));
    }

    [Fact]
    public void GetCollectionName_ActivityMemory_ReturnsExpected()
    {
        var options = new QdrantOptions();
        Assert.Equal("aura_activity_memory", options.GetCollectionName(SemanticCollectionType.ActivityMemory));
    }

    [Fact]
    public void GetCollectionName_CustomNames_ReturnsCustomValues()
    {
        var options = new QdrantOptions
        {
            ProjectKnowledgeCollection = "custom_pk",
            ActivityMemoryCollection = "custom_am"
        };

        Assert.Equal("custom_pk", options.GetCollectionName(SemanticCollectionType.ProjectKnowledge));
        Assert.Equal("custom_am", options.GetCollectionName(SemanticCollectionType.ActivityMemory));
    }

    [Fact]
    public void GetCollectionName_InvalidEnum_ThrowsArgumentOutOfRange()
    {
        var options = new QdrantOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.GetCollectionName((SemanticCollectionType)999));
    }

    [Fact]
    public void SectionName_IsQdrant()
    {
        Assert.Equal("Qdrant", QdrantOptions.SectionName);
    }
}
