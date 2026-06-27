using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class ComplianceTemplateConfiguration : IEntityTypeConfiguration<ComplianceTemplate>
{
    public void Configure(EntityTypeBuilder<ComplianceTemplate> builder)
    {
        builder.ToTable("ComplianceTemplates");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.FilingType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Name }).IsUnique();
    }
}

