using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxCompliance.Application.AuditLog;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Application.ComplianceTaskRules;
using TaxCompliance.Application.ComplianceTemplates;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Application.FileStorage;
using TaxCompliance.Application.Jurisdictions;
using TaxCompliance.Application.LegalEntities;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Application.Organizations;
using TaxCompliance.Application.Users;
using TaxCompliance.Infrastructure.BackgroundServices;
using TaxCompliance.Infrastructure.Authentication;
using TaxCompliance.Infrastructure.FileStorage;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Messaging;
using TaxCompliance.Infrastructure.Notifications;
using TaxCompliance.Infrastructure.Persistence;
using TaxCompliance.Infrastructure.Services;

namespace TaxCompliance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemoryInfrastructure = configuration.GetValue<bool>("Development:UseInMemoryInfrastructure");

        if (useInMemoryInfrastructure)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TaxComplianceDev"));
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
            });
        }

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = configuration.GetValue("Security:Lockout:MaxFailedAccessAttempts", 5);
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.GetValue("Security:Lockout:DefaultLockoutMinutes", 15));
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));
        services.Configure<OccurrenceGenerationOptions>(configuration.GetSection("OccurrenceGeneration"));
        services.Configure<LocalFileStorageOptions>(configuration.GetSection("FileStorage"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.Configure<NotificationProcessingOptions>(configuration.GetSection("Notifications"));
        services.Configure<EmailDeliveryOptions>(configuration.GetSection(EmailDeliveryOptions.SectionName));
        ConfigureDataProtection(services, configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IMfaService, TotpMfaService>();
        services.AddScoped<IMfaSecretProtector, DataProtectionMfaSecretProtector>();
        services.AddScoped<ICurrentUserContextService, CurrentUserContextService>();
        services.AddScoped<IOrganizationScopeService, OrganizationScopeService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ILegalEntityService, LegalEntityService>();
        services.AddScoped<IJurisdictionService, JurisdictionService>();
        services.AddScoped<IComplianceTemplateService, ComplianceTemplateService>();
        services.AddScoped<IComplianceTaskRuleService, ComplianceTaskRuleService>();
        services.AddScoped<IDueDateCalculationService, DueDateCalculationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDashboardCacheInvalidationService, DashboardCacheInvalidationService>();
        services.AddScoped<IComplianceTaskOccurrenceGenerationService, ComplianceTaskOccurrenceGenerationService>();
        services.AddScoped<IComplianceTaskOccurrenceWorkflowService, ComplianceTaskOccurrenceWorkflowService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        if (useInMemoryInfrastructure)
        {
            services.AddSingleton<ITaskNotificationPublisher, NoOpTaskNotificationPublisher>();
        }
        else
        {
            services.AddSingleton<ITaskNotificationPublisher>(provider =>
            {
                var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
                return new RabbitMqTaskNotificationPublisher(options);
            });
        }

        var emailProvider = configuration.GetValue("Email:Provider", environment.IsDevelopment() ? "Development" : "Smtp");
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase) || !environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, DevelopmentEmailSender>();
        }

        services.AddSingleton<IFileStorageService>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocalFileStorageOptions>>().Value;
            return new LocalFileStorageService(options);
        });
        services.AddHostedService<ComplianceTaskOccurrenceGenerationHostedService>();
        services.AddHostedService<TaskNotificationMonitorHostedService>();
        if (!useInMemoryInfrastructure)
        {
            services.AddHostedService<TaskNotificationConsumerHostedService>();
        }

        return services;
    }

    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var keysPath = configuration["DataProtection:KeysPath"];
        var dataProtection = services.AddDataProtection();

        if (string.IsNullOrWhiteSpace(keysPath))
        {
            return;
        }

        Directory.CreateDirectory(keysPath);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }
}
