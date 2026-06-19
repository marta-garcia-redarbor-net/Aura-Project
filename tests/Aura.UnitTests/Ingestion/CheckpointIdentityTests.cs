using Aura.Application.Models;
using System.Globalization;

namespace Aura.UnitTests.Ingestion;

public class CheckpointIdentityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ThrowsArgumentException_WhenConnectorIsNullOrEmpty(string? connector)
    {
        AssertInvalidIdentityPart(() => new CheckpointIdentity(connector!, "messages", "acme"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ThrowsArgumentException_WhenSourceIsNullOrEmpty(string? source)
    {
        AssertInvalidIdentityPart(() => new CheckpointIdentity("teams", source!, "acme"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ThrowsArgumentException_WhenTenantIsNullOrEmpty(string? tenant)
    {
        AssertInvalidIdentityPart(() => new CheckpointIdentity("teams", "messages", tenant!));
    }

    [Fact]
    public void Constructor_CreatesIdentity_WhenAllPartsAreValid()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");

        Assert.Equal("teams", identity.Connector);
        Assert.Equal("messages", identity.Source);
        Assert.Equal("acme", identity.Tenant);
    }

    [Fact]
    public void Constructor_AllowsBothNullableCheckpointFieldsToBeNull()
    {
        var checkpoint = new IngestionCheckpoint(null, null);

        Assert.Null(checkpoint.Cursor);
        Assert.Null(checkpoint.ProcessedAt);
    }

    [Fact]
    public void Record_UsesStructuralEquality_ForSameValues()
    {
        var expectedProcessedAt = DateTimeOffset.Parse("2026-06-18T10:00:00Z", CultureInfo.InvariantCulture);
        var first = new IngestionCheckpoint("delta-abc", expectedProcessedAt);
        var second = new IngestionCheckpoint("delta-abc", expectedProcessedAt);

        Assert.Equal(first, second);
    }

    private static void AssertInvalidIdentityPart(Action construct)
    {
        Assert.Throws<ArgumentException>(construct);
    }
}
