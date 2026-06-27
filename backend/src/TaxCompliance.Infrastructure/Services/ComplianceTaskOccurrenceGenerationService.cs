using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class ComplianceTaskOccurrenceGenerationService : IComplianceTaskOccurrenceGenerationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IDueDateCalculationService dueDateCalculationService;
    private readonly OccurrenceGenerationOptions options;
    private readonly ICurrentUserContextService currentUserContextService;
    private readonly IDashboardCacheInvalidationService dashboardCacheInvalidationService;

    public ComplianceTaskOccurrenceGenerationService(
        ApplicationDbContext dbContext,
        IDueDateCalculationService dueDateCalculationService,
        IOptions<OccurrenceGenerationOptions> options,
        ICurrentUserContextService currentUserContextService,
        IDashboardCacheInvalidationService dashboardCacheInvalidationService)
    {
        this.dbContext = dbContext;
        this.dueDateCalculationService = dueDateCalculationService;
        this.options = options.Value;
        this.currentUserContextService = currentUserContextService;
        this.dashboardCacheInvalidationService = dashboardCacheInvalidationService;
    }

    public async Task<OccurrenceGenerationResultDto> GenerateAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var generationEndDate = today.AddMonths(options.MonthsAheadToGenerate);
        var activeRules = await dbContext.ComplianceTaskRules
            .Where(rule => rule.IsActive)
            .ToListAsync(cancellationToken);

        var result = new OccurrenceGenerationResultDto
        {
            RulesEvaluated = activeRules.Count
        };

        foreach (var rule in activeRules)
        {
            var schedule = dueDateCalculationService.BuildSchedule(rule, today, generationEndDate);
            var periodStarts = schedule.Select(item => item.PeriodStartDate).ToArray();
            var currentUser = currentUserContextService.GetCurrentUser();

            var existingOccurrences = await dbContext.ComplianceTaskOccurrences
                .Where(occurrence => occurrence.ComplianceTaskRuleId == rule.Id && periodStarts.Contains(occurrence.PeriodStartDate))
                .Select(occurrence => new { occurrence.PeriodStartDate, occurrence.PeriodEndDate })
                .ToListAsync(cancellationToken);

            foreach (var scheduledOccurrence in schedule)
            {
                var duplicateExists = existingOccurrences.Any(existing =>
                    existing.PeriodStartDate == scheduledOccurrence.PeriodStartDate &&
                    existing.PeriodEndDate == scheduledOccurrence.PeriodEndDate);

                if (duplicateExists)
                {
                    result.DuplicateOccurrencesSkipped++;
                    continue;
                }

                var occurrence = new ComplianceTaskOccurrence
                {
                    ComplianceTaskRuleId = rule.Id,
                    AssignedToUserId = string.Empty,
                    PeriodStartDate = scheduledOccurrence.PeriodStartDate,
                    PeriodEndDate = scheduledOccurrence.PeriodEndDate,
                    DueDate = scheduledOccurrence.DueDate,
                    Status = ComplianceTaskOccurrenceStatus.Pending
                };

                dbContext.ComplianceTaskOccurrences.Add(occurrence);

                dbContext.AuditLogEntries.Add(new AuditLogEntry
                {
                    ComplianceTaskOccurrenceId = occurrence.Id,
                    ActionType = "TaskCreated",
                    Description = $"Task occurrence created for period {scheduledOccurrence.PeriodStartDate:yyyy-MM-dd} to {scheduledOccurrence.PeriodEndDate:yyyy-MM-dd}.",
                    PerformedByUserId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId,
                    PerformedByDisplayName = string.IsNullOrWhiteSpace(currentUser.DisplayName) ? "System" : currentUser.DisplayName
                });

                result.OccurrencesCreated++;
            }
        }

        if (result.OccurrencesCreated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await dashboardCacheInvalidationService.InvalidateAsync(cancellationToken);
        }

        return result;
    }
}
