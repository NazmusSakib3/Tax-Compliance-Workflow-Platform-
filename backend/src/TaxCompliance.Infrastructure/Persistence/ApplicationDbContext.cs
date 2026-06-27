using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Identity;

namespace TaxCompliance.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<Jurisdiction> Jurisdictions => Set<Jurisdiction>();
    public DbSet<ComplianceTemplate> ComplianceTemplates => Set<ComplianceTemplate>();
    public DbSet<ComplianceTaskRule> ComplianceTaskRules => Set<ComplianceTaskRule>();
    public DbSet<ComplianceTaskOccurrence> ComplianceTaskOccurrences => Set<ComplianceTaskOccurrence>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskDocument> TaskDocuments => Set<TaskDocument>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
