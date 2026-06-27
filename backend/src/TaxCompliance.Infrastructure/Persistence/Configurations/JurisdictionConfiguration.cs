using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class JurisdictionConfiguration : IEntityTypeConfiguration<Jurisdiction>
{
    public void Configure(EntityTypeBuilder<Jurisdiction> builder)
    {
        builder.ToTable("Jurisdictions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(entity => entity.RegionCode).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.FilingAuthority).HasMaxLength(150).IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.CountryCode, entity.RegionCode }).IsUnique();
    }
}

