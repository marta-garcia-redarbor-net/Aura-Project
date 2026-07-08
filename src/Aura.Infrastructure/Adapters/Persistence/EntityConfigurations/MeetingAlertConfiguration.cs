using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Adapters.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>MeetingAlerts</c> table.
/// Maps <see cref="MeetingAlertEntity"/> to the existing SQLite schema.
/// </summary>
public sealed class MeetingAlertConfiguration : IEntityTypeConfiguration<MeetingAlertEntity>
{
    public void Configure(EntityTypeBuilder<MeetingAlertEntity> builder)
    {
        builder.ToTable("MeetingAlerts");
        builder.HasKey(e => new { e.EventId, e.Trigger, e.LocalDate });
        builder.Property(e => e.EventId).HasColumnName("EventId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Trigger).HasColumnName("Trigger").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.LocalDate).HasColumnName("LocalDate").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Title).HasColumnName("Title").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.StartsAtUtc).HasColumnName("StartsAtUtc").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.JoinUrl).HasColumnName("JoinUrl").HasColumnType("TEXT");
        builder.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.HasBeenSent).HasColumnName("HasBeenSent").HasColumnType("INTEGER").IsRequired();
    }
}
