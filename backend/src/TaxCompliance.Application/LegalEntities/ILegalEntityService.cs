using TaxCompliance.Application.Common;
using TaxCompliance.Application.LegalEntities;

namespace TaxCompliance.Application.LegalEntities;

public interface ILegalEntityService
{
    Task<PagedResult<LegalEntityListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<LegalEntityDetailDto> GetByIdAsync(Guid legalEntityId, CancellationToken cancellationToken);
    Task<LegalEntityDetailDto> CreateAsync(SaveLegalEntityRequest request, CancellationToken cancellationToken);
    Task<LegalEntityDetailDto> UpdateAsync(Guid legalEntityId, SaveLegalEntityRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid legalEntityId, CancellationToken cancellationToken);
}
