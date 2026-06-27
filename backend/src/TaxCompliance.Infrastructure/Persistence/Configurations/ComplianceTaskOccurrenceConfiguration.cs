using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class ComplianceTaskOccurrenceConfiguration : IEntityTypeConfiguration<ComplianceTaskOccurrence>
{
    public void Configure(EntityTypeBuilder<ComplianceTaskOccurrence> builder)
    {
        builder.ToTable("ComplianceTaskOccurrences");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.AssignedToUserId).HasMaxLength(100);

        builder
            .HasOne(entity => entity.ComplianceTaskRule)
            .WithMany()
            .HasForeignKey(entity => entity.ComplianceTaskRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.ComplianceTaskRuleId, entity.PeriodStartDate, entity.PeriodEndDate }).IsUnique();
    }
}
