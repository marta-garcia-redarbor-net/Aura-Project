using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Adapters.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>WorkItems</c> table.
/// Maps <see cref="WorkItemEntity"/> to the existing SQLite schema.
/// </summary>
public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItemEntity>
{
    public void Configure(EntityTypeBuilder<WorkItemEntity> builder)
    {
        builder.ToTable("WorkItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").HasColumnType("TEXT");
        builder.Property(e => e.ExternalId).HasColumnName("ExternalId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Title).HasColumnName("Title").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Source).HasColumnName("Source").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.SourceType).HasColumnName("SourceType").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Priority).HasColumnName("Priority").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.MetadataJson).HasColumnName("MetadataJson").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.CorrelationId).HasColumnName("CorrelationId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.CapturedAtUtc).HasColumnName("CapturedAtUtc").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.SchemaVersion).HasColumnName("SchemaVersion").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Status).HasColumnName("Status").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("TEXT");
        builder.Property(e => e.FaultReason).HasColumnName("FaultReason").HasColumnType("TEXT");
        builder.Property(e => e.PriorityScore).HasColumnName("PriorityScore");
        builder.Property(e => e.OwnerUserId).HasColumnName("OwnerUserId").HasColumnType("TEXT");

        builder.HasIndex(e => e.ExternalId).IsUnique().HasDatabaseName("IX_WorkItems_ExternalId");
        builder.HasIndex(e => e.CapturedAtUtc).HasDatabaseName("IX_WorkItems_CapturedAtUtc");
    }
}
