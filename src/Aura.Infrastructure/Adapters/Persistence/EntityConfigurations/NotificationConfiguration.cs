using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Adapters.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>NotificationOutbox</c> table.
/// Maps <see cref="Notification"/> to the existing SQLite schema.
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("NotificationOutbox");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").HasColumnType("TEXT");
        builder.Property(e => e.WorkItemId).HasColumnName("WorkItemId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.SourceType).HasColumnName("SourceType").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Title).HasColumnName("Title").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Priority).HasColumnName("Priority").HasColumnType("REAL").IsRequired();
        builder.Property(e => e.TriggerRule).HasColumnName("TriggerRule").HasColumnType("TEXT");
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.DispatchedAt).HasColumnName("DispatchedAt").HasColumnType("TEXT");
        builder.Property(e => e.Explanation).HasColumnName("Explanation").HasColumnType("TEXT");
        builder.Property(e => e.Decision).HasColumnName("Decision").HasColumnType("TEXT");
        builder.Property(e => e.TargetUserId).HasColumnName("TargetUserId").HasColumnType("TEXT");
        builder.Property(e => e.RuleResults).HasColumnName("RuleResults").HasColumnType("TEXT");

        builder.HasIndex(e => e.DispatchedAt)
            .HasDatabaseName("IX_NotificationOutbox_Pending");
    }
}
