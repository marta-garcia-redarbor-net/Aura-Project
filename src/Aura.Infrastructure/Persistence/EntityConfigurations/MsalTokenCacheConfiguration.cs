using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>MsalTokenCache</c> table.
/// Maps <see cref="MsalTokenCacheEntry"/> to the existing SQLite schema.
/// </summary>
public sealed class MsalTokenCacheConfiguration : IEntityTypeConfiguration<MsalTokenCacheEntry>
{
    public void Configure(EntityTypeBuilder<MsalTokenCacheEntry> builder)
    {
        builder.ToTable("MsalTokenCache");
        builder.HasKey(e => e.CacheKey);
        builder.Property(e => e.CacheKey).HasColumnName("CacheKey").HasColumnType("TEXT");
        builder.Property(e => e.Data).HasColumnName("Data").HasColumnType("BLOB").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("TEXT").IsRequired();
    }
}
