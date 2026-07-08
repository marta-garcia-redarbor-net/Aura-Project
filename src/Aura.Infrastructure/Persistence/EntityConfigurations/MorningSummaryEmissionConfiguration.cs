using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the <c>MorningSummaryEmission</c> table.
/// Maps <see cref="MorningSummaryEmission"/> to the existing SQLite schema.
/// </summary>
public sealed class MorningSummaryEmissionConfiguration : IEntityTypeConfiguration<MorningSummaryEmission>
{
    public void Configure(EntityTypeBuilder<MorningSummaryEmission> builder)
    {
        builder.ToTable("MorningSummaryEmission");
        builder.HasKey(e => new { e.UserId, e.LocalDate });
        builder.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.LocalDate).HasColumnName("LocalDate").HasColumnType("TEXT").IsRequired();
        builder.Property(e => e.EmittedAt).HasColumnName("EmittedAt").HasColumnType("TEXT").IsRequired();
    }
}
