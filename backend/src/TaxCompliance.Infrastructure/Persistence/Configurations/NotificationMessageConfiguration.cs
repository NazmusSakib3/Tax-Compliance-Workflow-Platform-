using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("NotificationMessages");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.NotificationType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Subject).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(entity => new { entity.ComplianceTaskOccurrenceId, entity.NotificationType }).IsUnique();

        builder
            .HasOne(entity => entity.ComplianceTaskOccurrence)
            .WithMany()
            .HasForeignKey(entity => entity.ComplianceTaskOccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

