using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaxCompliance.Infrastructure.Persistence;

#nullable disable

namespace TaxCompliance.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
public partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", entity =>
        {
            entity.Property<string>("Id").HasColumnType("text");
            entity.Property<string>("ConcurrencyStamp").HasColumnType("text");
            entity.Property<string>("Name").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("NormalizedName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex");
            entity.ToTable("AspNetRoles");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer");
            entity.Property<string>("ClaimType").HasColumnType("text");
            entity.Property<string>("ClaimValue").HasColumnType("text");
            entity.Property<string>("RoleId").IsRequired().HasColumnType("text");
            entity.HasKey("Id");
            entity.HasIndex("RoleId");
            entity.ToTable("AspNetRoleClaims");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer");
            entity.Property<string>("ClaimType").HasColumnType("text");
            entity.Property<string>("ClaimValue").HasColumnType("text");
            entity.Property<string>("UserId").IsRequired().HasColumnType("text");
            entity.HasKey("Id");
            entity.HasIndex("UserId");
            entity.ToTable("AspNetUserClaims");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", entity =>
        {
            entity.Property<string>("LoginProvider").HasColumnType("text");
            entity.Property<string>("ProviderKey").HasColumnType("text");
            entity.Property<string>("ProviderDisplayName").HasColumnType("text");
            entity.Property<string>("UserId").IsRequired().HasColumnType("text");
            entity.HasKey("LoginProvider", "ProviderKey");
            entity.HasIndex("UserId");
            entity.ToTable("AspNetUserLogins");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", entity =>
        {
            entity.Property<string>("UserId").HasColumnType("text");
            entity.Property<string>("RoleId").HasColumnType("text");
            entity.HasKey("UserId", "RoleId");
            entity.HasIndex("RoleId");
            entity.ToTable("AspNetUserRoles");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", entity =>
        {
            entity.Property<string>("UserId").HasColumnType("text");
            entity.Property<string>("LoginProvider").HasColumnType("text");
            entity.Property<string>("Name").HasColumnType("text");
            entity.Property<string>("Value").HasColumnType("text");
            entity.HasKey("UserId", "LoginProvider", "Name");
            entity.ToTable("AspNetUserTokens");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.ComplianceTaskRule", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<Guid>("ComplianceTemplateId").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            entity.Property<int>("DueDayOfMonth").HasColumnType("integer");
            entity.Property<int?>("DueMonthOfYear").HasColumnType("integer");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<Guid>("JurisdictionId").HasColumnType("uuid");
            entity.Property<Guid>("LegalEntityId").HasColumnType("uuid");
            entity.Property<int>("RecurrenceType").HasColumnType("integer");
            entity.Property<string>("Title").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTemplateId");
            entity.HasIndex("JurisdictionId");
            entity.HasIndex("LegalEntityId");
            entity.ToTable("ComplianceTaskRules");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<string>("AssignedToUserId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<Guid>("ComplianceTaskRuleId").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateOnly>("DueDate").HasColumnType("date");
            entity.Property<DateOnly>("PeriodEndDate").HasColumnType("date");
            entity.Property<DateOnly>("PeriodStartDate").HasColumnType("date");
            entity.Property<int>("Status").HasColumnType("integer");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTaskRuleId", "PeriodStartDate", "PeriodEndDate").IsUnique();
            entity.ToTable("ComplianceTaskOccurrences");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.NotificationMessage", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<string>("Body").IsRequired().HasMaxLength(4000).HasColumnType("character varying(4000)");
            entity.Property<Guid>("ComplianceTaskOccurrenceId").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<bool>("IsProcessed").HasColumnType("boolean");
            entity.Property<string>("NotificationType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<DateTime?>("ProcessedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("RecipientEmail").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("Subject").IsRequired().HasMaxLength(300).HasColumnType("character varying(300)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTaskOccurrenceId", "NotificationType").IsUnique();
            entity.ToTable("NotificationMessages");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.TaskComment", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<Guid>("ComplianceTaskOccurrenceId").HasColumnType("uuid");
            entity.Property<string>("CommentText").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("CreatedByDisplayName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property<string>("CreatedByUserId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTaskOccurrenceId");
            entity.ToTable("TaskComments");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.TaskDocument", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<Guid>("ComplianceTaskOccurrenceId").HasColumnType("uuid");
            entity.Property<string>("ContentType").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("FileName").IsRequired().HasMaxLength(260).HasColumnType("character varying(260)");
            entity.Property<long>("FileSizeBytes").HasColumnType("bigint");
            entity.Property<string>("StoredPath").IsRequired().HasMaxLength(260).HasColumnType("character varying(260)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("UploadedByDisplayName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property<string>("UploadedByUserId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTaskOccurrenceId");
            entity.ToTable("TaskDocuments");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.AuditLogEntry", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<string>("ActionType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<Guid>("ComplianceTaskOccurrenceId").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("character varying(1000)");
            entity.Property<string>("PerformedByDisplayName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.Property<string>("PerformedByUserId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("ComplianceTaskOccurrenceId");
            entity.ToTable("AuditLogEntries");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.ComplianceTemplate", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            entity.Property<string>("FilingType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<int>("ReminderDaysBeforeDue").HasColumnType("integer");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("Name").IsUnique();
            entity.ToTable("ComplianceTemplates");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.Jurisdiction", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("CountryCode").IsRequired().HasMaxLength(2).HasColumnType("character varying(2)");
            entity.Property<string>("FilingAuthority").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<string>("RegionCode").IsRequired().HasMaxLength(10).HasColumnType("character varying(10)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("CountryCode", "RegionCode").IsUnique();
            entity.ToTable("Jurisdictions");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.LegalEntity", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<Guid>("OrganizationId").HasColumnType("uuid");
            entity.Property<string>("RegistrationNumber").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<string>("TaxIdentifier").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("OrganizationId");
            entity.HasIndex("RegistrationNumber").IsUnique();
            entity.HasIndex("TaxIdentifier").IsUnique();
            entity.ToTable("LegalEntities");
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.Organization", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid");
            entity.Property<string>("Code").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            entity.Property<DateTime>("CreatedUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            entity.Property<bool>("IsActive").HasColumnType("boolean");
            entity.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<DateTime?>("UpdatedUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("Code").IsUnique();
            entity.ToTable("Organizations");
        });

        modelBuilder.Entity("TaxCompliance.Infrastructure.Identity.ApplicationUser", entity =>
        {
            entity.Property<string>("Id").HasColumnType("text");
            entity.Property<int>("AccessFailedCount").HasColumnType("integer");
            entity.Property<string>("ConcurrencyStamp").HasColumnType("text");
            entity.Property<string>("DisplayName").IsRequired().HasColumnType("text");
            entity.Property<string>("Email").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<bool>("EmailConfirmed").HasColumnType("boolean");
            entity.Property<bool>("LockoutEnabled").HasColumnType("boolean");
            entity.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("timestamp with time zone");
            entity.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("NormalizedUserName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.Property<string>("PasswordHash").HasColumnType("text");
            entity.Property<string>("PhoneNumber").HasColumnType("text");
            entity.Property<bool>("PhoneNumberConfirmed").HasColumnType("boolean");
            entity.Property<string>("SecurityStamp").HasColumnType("text");
            entity.Property<bool>("TwoFactorEnabled").HasColumnType("boolean");
            entity.Property<string>("UserName").HasMaxLength(256).HasColumnType("character varying(256)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex");
            entity.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex");
            entity.ToTable("AspNetUsers");
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", entity =>
        {
            entity.HasOne("TaxCompliance.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", entity =>
        {
            entity.HasOne("TaxCompliance.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", entity =>
        {
            entity.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne("TaxCompliance.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", entity =>
        {
            entity.HasOne("TaxCompliance.Infrastructure.Identity.ApplicationUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.ComplianceTaskRule", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTemplate", "ComplianceTemplate")
                .WithMany("ComplianceTaskRules")
                .HasForeignKey("ComplianceTemplateId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("TaxCompliance.Domain.Entities.Jurisdiction", "Jurisdiction")
                .WithMany("ComplianceTaskRules")
                .HasForeignKey("JurisdictionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne("TaxCompliance.Domain.Entities.LegalEntity", "LegalEntity")
                .WithMany("ComplianceTaskRules")
                .HasForeignKey("LegalEntityId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTaskRule", "ComplianceTaskRule")
                .WithMany()
                .HasForeignKey("ComplianceTaskRuleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.NotificationMessage", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", "ComplianceTaskOccurrence")
                .WithMany()
                .HasForeignKey("ComplianceTaskOccurrenceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.TaskComment", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", "ComplianceTaskOccurrence")
                .WithMany("Comments")
                .HasForeignKey("ComplianceTaskOccurrenceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.TaskDocument", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", "ComplianceTaskOccurrence")
                .WithMany("Documents")
                .HasForeignKey("ComplianceTaskOccurrenceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.AuditLogEntry", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.ComplianceTaskOccurrence", "ComplianceTaskOccurrence")
                .WithMany("AuditLogEntries")
                .HasForeignKey("ComplianceTaskOccurrenceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("TaxCompliance.Domain.Entities.LegalEntity", entity =>
        {
            entity.HasOne("TaxCompliance.Domain.Entities.Organization", "Organization")
                .WithMany("LegalEntities")
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
    }
}
