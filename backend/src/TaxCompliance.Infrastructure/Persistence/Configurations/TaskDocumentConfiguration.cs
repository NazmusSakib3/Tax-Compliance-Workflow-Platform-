using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class TaskDocumentConfiguration : IEntityTypeConfiguration<TaskDocument>
{
    public void Configure(EntityTypeBuilder<TaskDocument> builder)
    {
        builder.ToTable("TaskDocuments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.FileName).HasMaxLength(260).IsRequired();
        builder.Property(entity => entity.StoredPath).HasMaxLength(260).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.UploadedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.UploadedByDisplayName).HasMaxLength(200).IsRequired();

        builder
            .HasOne(entity => entity.ComplianceTaskOccurrence)
            .WithMany(occurrence => occurrence.Documents)
            .HasForeignKey(entity => entity.ComplianceTaskOccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

