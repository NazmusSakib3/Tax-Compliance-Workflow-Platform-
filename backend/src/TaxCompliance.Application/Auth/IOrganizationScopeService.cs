namespace TaxCompliance.Application.Auth;

public interface IOrganizationScopeService
{
    Guid? GetOrganizationId();

    Guid RequireOrganizationId();

    bool HasOrganizationScope();

    void EnsureSameOrganization(Guid organizationId);
}
