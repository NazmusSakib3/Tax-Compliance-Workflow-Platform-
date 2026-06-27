using TaxCompliance.Application.Dashboard;

namespace TaxCompliance.Tests.TestDoubles;

public class FakeDashboardCacheInvalidationService : IDashboardCacheInvalidationService
{
    public int InvalidationCount { get; private set; }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        InvalidationCount++;
        return Task.CompletedTask;
    }
}

