using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>SemanticOutbox</c> table.
/// Maps <see cref="SemanticOutboxEntryEntity"/> to the existing SQLite schema.
/// </summary>
public sealed class SemanticOutboxConfiguration : IEntityTypeConfiguration<SemanticOutboxEntryEntity>
{
    public void Configure(EntityTypeBuilder<SemanticOutboxEntryEntity> builder)
    {
        builder.ToTable("SemanticOutbox");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").HasColumnType("TEXT");
        builder.Property(e => e.CanonicalSourceId).HasColumnName("CanonicalSourceId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Content).HasColumnName("Content").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Collection).HasColumnName("Collection").HasColumnType("INTEGER").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Processed).HasColumnName("Processed").HasColumnType("INTEGER").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("ProcessedAt").HasColumnType("TEXT");
        builder.Property(e => e.Error).HasColumnName("Error").HasColumnType("TEXT");

        builder.HasIndex(e => new { e.Processed, e.CreatedAt })
            .HasFilter("[Processed] = 0")
            .HasDatabaseName("IX_SemanticOutbox_Pending");
    }
}
