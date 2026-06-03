namespace Aura.UnitTests.SemanticIndex;

using Aura.Domain.SemanticIndex.Enums;

public class SemanticCollectionTypeTests
{
    [Fact]
    public void ProjectKnowledge_HasExpectedValue()
    {
        var collection = SemanticCollectionType.ProjectKnowledge;
        Assert.Equal(SemanticCollectionType.ProjectKnowledge, collection);
    }

    [Fact]
    public void ActivityMemory_HasExpectedValue()
    {
        var collection = SemanticCollectionType.ActivityMemory;
        Assert.Equal(SemanticCollectionType.ActivityMemory, collection);
    }

    [Fact]
    public void Enum_HasExactlyTwoMembers()
    {
        var values = Enum.GetValues<SemanticCollectionType>();
        Assert.Equal(2, values.Length);
    }

    [Theory]
    [InlineData(SemanticCollectionType.ProjectKnowledge, "ProjectKnowledge")]
    [InlineData(SemanticCollectionType.ActivityMemory, "ActivityMemory")]
    public void Enum_ToString_ReturnsExpectedName(SemanticCollectionType collection, string expected)
    {
        Assert.Equal(expected, collection.ToString());
    }
}
