using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TaxCompliance.Application.Auth;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Seeding;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var defaultOrganization = await dbContext.Organizations
            .SingleOrDefaultAsync(organization => organization.Code == "PLATFORM");

        if (defaultOrganization is null)
        {
            defaultOrganization = new Organization
            {
                Name = "Platform Organization",
                Code = "PLATFORM",
                Description = "Default organization for seeded users and local development.",
                IsActive = true
            };

            dbContext.Organizations.Add(defaultOrganization);
            await dbContext.SaveChangesAsync();
        }

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        var adminDisplayName = configuration["Seed:AdminDisplayName"] ?? "Platform Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Seed:AdminEmail and Seed:AdminPassword must be configured outside Development.");
            }

            adminEmail = "admin@taxplatform.local";
            adminPassword = "Admin123!";
        }

        var existingAdmin = await userManager.Users.SingleOrDefaultAsync(user => user.Email == adminEmail);
        if (existingAdmin is not null)
        {
            if (existingAdmin.OrganizationId.HasValue || !existingAdmin.LockoutEnabled)
            {
                existingAdmin.OrganizationId = null;
                existingAdmin.LockoutEnabled = true;
                await userManager.UpdateAsync(existingAdmin);
            }

            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = adminDisplayName,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }

        await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
    }
}
