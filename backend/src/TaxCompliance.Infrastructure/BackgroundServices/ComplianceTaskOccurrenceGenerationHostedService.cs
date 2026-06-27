using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaxCompliance.Application.ComplianceTaskOccurrences;

namespace TaxCompliance.Infrastructure.BackgroundServices;

public class ComplianceTaskOccurrenceGenerationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ComplianceTaskOccurrenceGenerationHostedService> logger;

    public ComplianceTaskOccurrenceGenerationHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ComplianceTaskOccurrenceGenerationHostedService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<IComplianceTaskOccurrenceGenerationService>();
            var result = await generator.GenerateAsync(stoppingToken);
            logger.LogInformation(
                "Occurrence generation completed. Rules: {RulesEvaluated}, Created: {OccurrencesCreated}, DuplicatesSkipped: {DuplicateOccurrencesSkipped}",
                result.RulesEvaluated,
                result.OccurrencesCreated,
                result.DuplicateOccurrencesSkipped);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
