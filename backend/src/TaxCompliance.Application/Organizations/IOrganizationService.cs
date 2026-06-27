using TaxCompliance.Application.Common;
using TaxCompliance.Application.Organizations;

namespace TaxCompliance.Application.Organizations;

public interface IOrganizationService
{
    Task<PagedResult<OrganizationListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<OrganizationDetailDto> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<OrganizationDetailDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken);
    Task<OrganizationDetailDto> UpdateAsync(Guid organizationId, SaveOrganizationRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid organizationId, CancellationToken cancellationToken);
}
