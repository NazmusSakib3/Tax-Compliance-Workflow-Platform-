using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ActionType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.PerformedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PerformedByDisplayName).HasMaxLength(200).IsRequired();

        builder
            .HasOne(entity => entity.ComplianceTaskOccurrence)
            .WithMany(occurrence => occurrence.AuditLogEntries)
            .HasForeignKey(entity => entity.ComplianceTaskOccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

