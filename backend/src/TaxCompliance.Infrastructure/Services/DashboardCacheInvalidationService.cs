using Microsoft.Extensions.Caching.Distributed;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Infrastructure.Caching;

namespace TaxCompliance.Infrastructure.Services;

public class DashboardCacheInvalidationService : IDashboardCacheInvalidationService
{
    private readonly IDistributedCache distributedCache;

    public DashboardCacheInvalidationService(IDistributedCache distributedCache)
    {
        this.distributedCache = distributedCache;
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        var versionKey = DashboardCacheKeys.SummaryVersion;
        var current = await distributedCache.GetStringAsync(versionKey, cancellationToken);
        var nextVersion = (long.TryParse(current, out var version) ? version : 0L) + 1;
        await distributedCache.SetStringAsync(versionKey, nextVersion.ToString(), cancellationToken);
    }
}

