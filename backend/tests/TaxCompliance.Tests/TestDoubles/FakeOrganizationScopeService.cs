using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;

namespace TaxCompliance.Tests.TestDoubles;

public class FakeOrganizationScopeService : IOrganizationScopeService
{
    public Guid? OrganizationId { get; set; }

    public Guid? GetOrganizationId() => OrganizationId;

    public Guid RequireOrganizationId() => OrganizationId ?? throw new InvalidOperationException("Organization is required.");

    public bool HasOrganizationScope() => OrganizationId.HasValue;

    public void EnsureSameOrganization(Guid organizationId)
    {
        if (OrganizationId.HasValue && OrganizationId.Value != organizationId)
        {
            throw new EntityNotFoundException("The requested resource was not found.");
        }
    }
}
