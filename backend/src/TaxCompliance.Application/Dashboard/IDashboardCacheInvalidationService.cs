namespace TaxCompliance.Application.Dashboard;

public interface IDashboardCacheInvalidationService
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}

