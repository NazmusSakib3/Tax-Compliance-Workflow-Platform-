using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaxCompliance.Infrastructure.BackgroundServices;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Tests.Integration;

public class TaxComplianceApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"TaxComplianceTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty,
                ["ConnectionStrings:Redis"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(IDistributedCache));

            RemoveHostedService<ComplianceTaskOccurrenceGenerationHostedService>(services);
            RemoveHostedService<TaskNotificationMonitorHostedService>(services);
            RemoveHostedService<TaskNotificationConsumerHostedService>(services);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddDistributedMemoryCache();
        });
    }

    private static void RemoveHostedService<THostedService>(IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(THostedService))
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
