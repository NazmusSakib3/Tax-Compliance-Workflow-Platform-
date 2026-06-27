using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class ComplianceTaskRuleConfiguration : IEntityTypeConfiguration<ComplianceTaskRule>
{
    public void Configure(EntityTypeBuilder<ComplianceTaskRule> builder)
    {
        builder.ToTable("ComplianceTaskRules");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Title).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);

        builder
            .HasOne(entity => entity.LegalEntity)
            .WithMany(legalEntity => legalEntity.ComplianceTaskRules)
            .HasForeignKey(entity => entity.LegalEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(entity => entity.Jurisdiction)
            .WithMany(jurisdiction => jurisdiction.ComplianceTaskRules)
            .HasForeignKey(entity => entity.JurisdictionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(entity => entity.ComplianceTemplate)
            .WithMany(template => template.ComplianceTaskRules)
            .HasForeignKey(entity => entity.ComplianceTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

