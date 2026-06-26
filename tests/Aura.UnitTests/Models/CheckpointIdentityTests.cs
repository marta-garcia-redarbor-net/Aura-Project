using Aura.Application.Models;

namespace Aura.UnitTests.Models;

public class CheckpointIdentityTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-123");

        Assert.Equal("teams", identity.Connector);
        Assert.Equal("messages", identity.Source);
        Assert.Equal("acme", identity.Tenant);
        Assert.Equal("oid-123", identity.UserOid);
    }

    [Fact]
    public void Constructor_WithoutUserOid_IsNull()
    {
        var identity = new CheckpointIdentity("outlook", "inbox", "contoso");

        Assert.Null(identity.UserOid);
        Assert.Equal("outlook", identity.Connector);
        Assert.Equal("inbox", identity.Source);
        Assert.Equal("contoso", identity.Tenant);
    }

    [Fact]
    public void CheckpointIdentity_WithUserOid_PropagatesValue()
    {
        var identity = new CheckpointIdentity("calendar", "calendar", "default", userOid: "oid-calendar-user");

        Assert.Equal("oid-calendar-user", identity.UserOid);
    }

    [Fact]
    public void CheckpointIdentity_WithoutUserOid_IsNull()
    {
        var identity = new CheckpointIdentity("teams", "messages", "tenant-a");

        Assert.Null(identity.UserOid);
    }

    [Fact]
    public void Constructor_EmptyConnector_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new CheckpointIdentity("", "messages", "tenant"));
    }

    [Fact]
    public void Constructor_EmptySource_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new CheckpointIdentity("teams", "", "tenant"));
    }

    [Fact]
    public void Constructor_EmptyTenant_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new CheckpointIdentity("teams", "messages", ""));
    }

    [Fact]
    public void Equality_SameValues_ReturnsEqual()
    {
        var a = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-1");
        var b = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-1");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentOid_ReturnsNotEqual()
    {
        var a = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-1");
        var b = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_OneNullOid_ReturnsNotEqual()
    {
        var a = new CheckpointIdentity("teams", "messages", "acme", userOid: "oid-1");
        var b = new CheckpointIdentity("teams", "messages", "acme");

        Assert.NotEqual(a, b);
    }
}
