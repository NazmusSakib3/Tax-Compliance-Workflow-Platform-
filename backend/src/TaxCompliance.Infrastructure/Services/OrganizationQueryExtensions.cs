using System.Linq.Expressions;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;

namespace TaxCompliance.Infrastructure.Services;

public static class OrganizationQueryExtensions
{
    public static bool IsPlatformAdmin(CurrentUserContext user) =>
        user.Roles.Contains(RoleNames.Admin) && !user.OrganizationId.HasValue;

    public static IQueryable<T> ApplyOrganizationScope<T>(
        this IQueryable<T> query,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService,
        Expression<Func<T, Guid>> organizationSelector)
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        var organizationId = organizationScope.GetOrganizationId();
        if (organizationId.HasValue)
        {
            var parameter = organizationSelector.Parameters[0];
            var body = Expression.Equal(organizationSelector.Body, Expression.Constant(organizationId.Value));
            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);
            return query.Where(predicate);
        }

        if (!IsPlatformAdmin(currentUser))
        {
            organizationScope.RequireOrganizationId();
        }

        return query;
    }
}
