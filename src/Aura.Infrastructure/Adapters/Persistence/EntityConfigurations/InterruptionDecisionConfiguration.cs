using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Adapters.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>InterruptionDecisions</c> table.
/// Maps <see cref="InterruptionDecision"/> to the existing SQLite schema.
/// </summary>
public sealed class InterruptionDecisionConfiguration : IEntityTypeConfiguration<InterruptionDecision>
{
    public void Configure(EntityTypeBuilder<InterruptionDecision> builder)
    {
        builder.ToTable("InterruptionDecisions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").HasColumnType("TEXT");
        builder.Property(e => e.WorkItemId).HasColumnName("WorkItemId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Title).HasColumnName("Title").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.SourceType).HasColumnName("SourceType").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.Decision).HasColumnName("Decision").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.PriorityScore).HasColumnName("PriorityScore");
        builder.Property(e => e.Explanation).HasColumnName("Explanation").HasColumnType("TEXT");
        builder.Property(e => e.Timestamp).HasColumnName("Timestamp").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.FocusState).HasColumnName("FocusState").HasColumnType("TEXT").IsRequired();

        builder.HasIndex(e => e.Timestamp).IsDescending().HasDatabaseName("IX_InterruptionDecisions_Timestamp");
    }
}
