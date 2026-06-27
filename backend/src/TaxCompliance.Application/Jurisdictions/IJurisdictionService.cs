using TaxCompliance.Application.Common;
using TaxCompliance.Application.Jurisdictions;

namespace TaxCompliance.Application.Jurisdictions;

public interface IJurisdictionService
{
    Task<PagedResult<JurisdictionListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<JurisdictionDetailDto> GetByIdAsync(Guid jurisdictionId, CancellationToken cancellationToken);
    Task<JurisdictionDetailDto> CreateAsync(SaveJurisdictionRequest request, CancellationToken cancellationToken);
    Task<JurisdictionDetailDto> UpdateAsync(Guid jurisdictionId, SaveJurisdictionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid jurisdictionId, CancellationToken cancellationToken);
}
