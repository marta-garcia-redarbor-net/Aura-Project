using System.Text.Json;
using Aura.Application.Models;

namespace Aura.UnitTests.Identity;

/// <summary>
/// Unit tests for <see cref="AuraUser"/> model changes.
/// Verifies backward compatibility when adding Oid and TenantId optional properties.
/// </summary>
public class AuraUserTests
{
    [Fact]
    public void AuraUser_NewOptionalFields_DefaultToNull()
    {
        // Arrange & Act
        var user = new AuraUser
        {
            UserId = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com"
        };

        // Assert — new fields should default to null
        Assert.Null(user.Oid);
        Assert.Null(user.TenantId);
    }

    [Fact]
    public void AuraUser_WithOidAndTenantId_SerializesCorrectly()
    {
        // Arrange
        var user = new AuraUser
        {
            UserId = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com",
            Oid = "oid-abc-123",
            TenantId = "tenant-xyz"
        };

        // Act
        var json = JsonSerializer.Serialize(user);
        var deserialized = JsonSerializer.Deserialize<AuraUser>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("user-123", deserialized.UserId);
        Assert.Equal("oid-abc-123", deserialized.Oid);
        Assert.Equal("tenant-xyz", deserialized.TenantId);
    }

    [Fact]
    public void AuraUser_BackwardCompatible_WithoutNewFields()
    {
        // Arrange — simulate old JSON without Oid/TenantId
        const string oldJson = """
            {"UserId":"old-user","DisplayName":"Old","Email":"old@test.com"}
            """;

        // Act
        var user = JsonSerializer.Deserialize<AuraUser>(oldJson);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("old-user", user.UserId);
        Assert.Null(user.Oid);
        Assert.Null(user.TenantId);
    }

    [Fact]
    public void AuraUser_RecordEquality_IgnoresOidAndTenantId()
    {
        // Arrange
        var user1 = new AuraUser { UserId = "u1", DisplayName = "A", Email = "a@b.com" };
        var user2 = new AuraUser { UserId = "u1", DisplayName = "A", Email = "a@b.com", Oid = "oid-diff" };

        // Assert — records with different optional fields are NOT equal
        Assert.NotEqual(user1, user2);
    }
}
