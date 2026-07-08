using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>FocusStateOverrides</c> table.
/// Maps <see cref="FocusStateOverride"/> to the existing SQLite schema.
/// </summary>
public sealed class FocusStateOverrideConfiguration : IEntityTypeConfiguration<FocusStateOverride>
{
    public void Configure(EntityTypeBuilder<FocusStateOverride> builder)
    {
        builder.ToTable("FocusStateOverrides");
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("TEXT");
        builder.Property(e => e.State).HasColumnName("State").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("TEXT");
    }
}
