using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        builder.ToTable("LegalEntities");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.RegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.TaxIdentifier).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.RegistrationNumber).IsUnique();
        builder.HasIndex(entity => entity.TaxIdentifier).IsUnique();

        builder
            .HasOne(entity => entity.Organization)
            .WithMany(organization => organization.LegalEntities)
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

