using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Infrastructure.Persistence.Configurations;

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CommentText).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.CreatedByDisplayName).HasMaxLength(200).IsRequired();

        builder
            .HasOne(entity => entity.ComplianceTaskOccurrence)
            .WithMany(occurrence => occurrence.Comments)
            .HasForeignKey(entity => entity.ComplianceTaskOccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

