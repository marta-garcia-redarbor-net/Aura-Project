namespace Aura.UnitTests.SemanticIndex;

using Aura.Domain.SemanticIndex.ValueObjects;

public class DomainTagTests
{
    [Fact]
    public void Constructor_SetsKeyAndValue()
    {
        var tag = new DomainTag("area", "backend");
        Assert.Equal("area", tag.Key);
        Assert.Equal("backend", tag.Value);
    }

    [Fact]
    public void Equality_SameKeyAndValue_AreEqual()
    {
        var tag1 = new DomainTag("area", "backend");
        var tag2 = new DomainTag("area", "backend");
        Assert.Equal(tag1, tag2);
    }

    [Fact]
    public void Equality_DifferentKey_AreNotEqual()
    {
        var tag1 = new DomainTag("area", "backend");
        var tag2 = new DomainTag("team", "backend");
        Assert.NotEqual(tag1, tag2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var tag1 = new DomainTag("area", "backend");
        var tag2 = new DomainTag("area", "frontend");
        Assert.NotEqual(tag1, tag2);
    }

    [Fact]
    public void GetHashCode_SameKeyAndValue_SameHash()
    {
        var tag1 = new DomainTag("area", "backend");
        var tag2 = new DomainTag("area", "backend");
        Assert.Equal(tag1.GetHashCode(), tag2.GetHashCode());
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData("key", null)]
    [InlineData("key", "")]
    public void Constructor_EmptyOrNullKeyOrValue_ThrowsArgumentException(string? key, string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DomainTag(key!, value!));
    }

    [Fact]
    public void ToString_ReturnsKeyColonValue()
    {
        var tag = new DomainTag("area", "backend");
        Assert.Equal("area:backend", tag.ToString());
    }
}
