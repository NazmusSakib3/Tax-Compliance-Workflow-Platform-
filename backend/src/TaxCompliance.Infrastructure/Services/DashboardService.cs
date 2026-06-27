using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Caching;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IDistributedCache distributedCache;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public DashboardService(
        ApplicationDbContext dbContext,
        IDistributedCache distributedCache,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.distributedCache = distributedCache;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var cacheKey = await BuildCacheKeyAsync(cancellationToken);
        var cached = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return JsonSerializer.Deserialize<DashboardSummaryDto>(cached) ?? new DashboardSummaryDto();
        }

        var summary = await BuildSummaryAsync(cancellationToken);

        await distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);

        return summary;
    }

    public async Task<byte[]> ExportComplianceReportAsync(CancellationToken cancellationToken)
    {
        var summary = await GetSummaryAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Metric,Value");
        builder.AppendLine($"Overdue tasks,{summary.OverdueCount}");
        builder.AppendLine($"Due soon,{summary.DueSoonCount}");
        builder.AppendLine($"Completed,{summary.CompletedCount}");
        builder.AppendLine($"In progress,{summary.InProgressCount}");
        builder.AppendLine($"Assigned to me,{summary.AssignedToMeCount}");
        builder.AppendLine($"Completed last 30 days,{summary.CompletedLast30Days}");
        builder.AppendLine($"Completed previous 30 days,{summary.CompletedPrevious30Days}");
        builder.AppendLine();
        builder.AppendLine("Jurisdiction,Open tasks,Overdue tasks");
        foreach (var item in summary.JurisdictionBreakdown)
        {
            builder.AppendLine($"{EscapeCsv(item.Name)},{item.OpenCount},{item.OverdueCount}");
        }

        builder.AppendLine();
        builder.AppendLine("Legal entity,Open tasks,Overdue tasks");
        foreach (var item in summary.LegalEntityBreakdown)
        {
            builder.AppendLine($"{EscapeCsv(item.Name)},{item.OpenCount},{item.OverdueCount}");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private async Task<DashboardSummaryDto> BuildSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueSoonLimit = today.AddDays(7);
        var last30DaysStart = today.AddDays(-30);
        var previous30DaysStart = today.AddDays(-60);
        var currentUser = currentUserContextService.GetCurrentUser();
        var occurrenceQuery = ScopedOccurrenceQuery();

        var counts = await occurrenceQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                OverdueCount = group.Count(occurrence =>
                    occurrence.DueDate < today &&
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Completed &&
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Cancelled),
                DueSoonCount = group.Count(occurrence =>
                    occurrence.DueDate >= today &&
                    occurrence.DueDate <= dueSoonLimit &&
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Completed &&
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Cancelled),
                CompletedCount = group.Count(occurrence =>
                    occurrence.Status == ComplianceTaskOccurrenceStatus.Completed),
                InProgressCount = group.Count(occurrence =>
                    occurrence.Status == ComplianceTaskOccurrenceStatus.InProgress),
                AssignedToMeCount = string.IsNullOrWhiteSpace(currentUser.UserId)
                    ? 0
                    : group.Count(occurrence =>
                        occurrence.AssignedToUserId == currentUser.UserId &&
                        (occurrence.Status == ComplianceTaskOccurrenceStatus.Draft ||
                         occurrence.Status == ComplianceTaskOccurrenceStatus.Pending ||
                         occurrence.Status == ComplianceTaskOccurrenceStatus.InProgress ||
                         occurrence.Status == ComplianceTaskOccurrenceStatus.Overdue)),
                CompletedLast30Days = group.Count(occurrence =>
                    occurrence.Status == ComplianceTaskOccurrenceStatus.Completed &&
                    occurrence.DueDate >= last30DaysStart &&
                    occurrence.DueDate <= today),
                CompletedPrevious30Days = group.Count(occurrence =>
                    occurrence.Status == ComplianceTaskOccurrenceStatus.Completed &&
                    occurrence.DueDate >= previous30DaysStart &&
                    occurrence.DueDate < last30DaysStart)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var summary = new DashboardSummaryDto
        {
            OverdueCount = counts?.OverdueCount ?? 0,
            DueSoonCount = counts?.DueSoonCount ?? 0,
            CompletedCount = counts?.CompletedCount ?? 0,
            InProgressCount = counts?.InProgressCount ?? 0,
            AssignedToMeCount = counts?.AssignedToMeCount ?? 0,
            CompletedLast30Days = counts?.CompletedLast30Days ?? 0,
            CompletedPrevious30Days = counts?.CompletedPrevious30Days ?? 0,
            JurisdictionBreakdown = await BuildJurisdictionBreakdownAsync(occurrenceQuery, today, cancellationToken),
            LegalEntityBreakdown = await BuildLegalEntityBreakdownAsync(occurrenceQuery, today, cancellationToken)
        };

        return summary;
    }

    private static async Task<List<DashboardBreakdownItemDto>> BuildJurisdictionBreakdownAsync(
        IQueryable<Domain.Entities.ComplianceTaskOccurrence> occurrenceQuery,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        return await occurrenceQuery
            .Select(occurrence => new
            {
                Name = occurrence.ComplianceTaskRule!.Jurisdiction!.Name,
                occurrence.DueDate,
                occurrence.Status
            })
            .GroupBy(item => item.Name)
            .Select(group => new DashboardBreakdownItemDto
            {
                Name = group.Key,
                OpenCount = group.Count(item =>
                    item.Status == ComplianceTaskOccurrenceStatus.Draft ||
                    item.Status == ComplianceTaskOccurrenceStatus.Pending ||
                    item.Status == ComplianceTaskOccurrenceStatus.InProgress ||
                    item.Status == ComplianceTaskOccurrenceStatus.Overdue),
                OverdueCount = group.Count(item =>
                    item.DueDate < today &&
                    item.Status != ComplianceTaskOccurrenceStatus.Completed &&
                    item.Status != ComplianceTaskOccurrenceStatus.Cancelled)
            })
            .OrderByDescending(item => item.OverdueCount)
            .ThenBy(item => item.Name)
            .Take(8)
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<DashboardBreakdownItemDto>> BuildLegalEntityBreakdownAsync(
        IQueryable<Domain.Entities.ComplianceTaskOccurrence> occurrenceQuery,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        return await occurrenceQuery
            .Select(occurrence => new
            {
                Name = occurrence.ComplianceTaskRule!.LegalEntity!.Name,
                occurrence.DueDate,
                occurrence.Status
            })
            .GroupBy(item => item.Name)
            .Select(group => new DashboardBreakdownItemDto
            {
                Name = group.Key,
                OpenCount = group.Count(item =>
                    item.Status == ComplianceTaskOccurrenceStatus.Draft ||
                    item.Status == ComplianceTaskOccurrenceStatus.Pending ||
                    item.Status == ComplianceTaskOccurrenceStatus.InProgress ||
                    item.Status == ComplianceTaskOccurrenceStatus.Overdue),
                OverdueCount = group.Count(item =>
                    item.DueDate < today &&
                    item.Status != ComplianceTaskOccurrenceStatus.Completed &&
                    item.Status != ComplianceTaskOccurrenceStatus.Cancelled)
            })
            .OrderByDescending(item => item.OverdueCount)
            .ThenBy(item => item.Name)
            .Take(8)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Domain.Entities.ComplianceTaskOccurrence> ScopedOccurrenceQuery()
    {
        return dbContext.ComplianceTaskOccurrences
            .AsQueryable()
            .ApplyOrganizationScope(
                organizationScope,
                currentUserContextService,
                occurrence => occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId);
    }

    private async Task<string> BuildCacheKeyAsync(CancellationToken cancellationToken)
    {
        var version = await distributedCache.GetStringAsync(DashboardCacheKeys.SummaryVersion, cancellationToken);
        var organizationId = organizationScope.GetOrganizationId();
        var userId = currentUserContextService.GetCurrentUser().UserId;
        var scopeSuffix = organizationId.HasValue ? $":{organizationId.Value:N}" : ":all";
        var userSuffix = string.IsNullOrWhiteSpace(userId) ? string.Empty : $":{userId}";
        return $"{DashboardCacheKeys.Summary}:v{version ?? "0"}{scopeSuffix}{userSuffix}";
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
