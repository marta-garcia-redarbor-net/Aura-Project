using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Adapters.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>AlertRules</c> table (mapped from VipSenders and AlertKeywords).
/// Uses TPH with a discriminator column to distinguish rule types.
/// </summary>
public sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasColumnName("Key").HasColumnType("TEXT");
        builder.Property(e => e.Value).HasColumnName("Value").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.AddedBy).HasColumnName("AddedBy").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.RuleType).HasColumnName("RuleType").HasColumnType("TEXT").IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(e => e.RuleType).HasDatabaseName("IX_AlertRules_RuleType");
    }
}
