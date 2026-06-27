using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTemplates;

namespace TaxCompliance.Application.ComplianceTemplates;

public interface IComplianceTemplateService
{
    Task<PagedResult<ComplianceTemplateListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<ComplianceTemplateDetailDto> GetByIdAsync(Guid complianceTemplateId, CancellationToken cancellationToken);
    Task<ComplianceTemplateDetailDto> CreateAsync(SaveComplianceTemplateRequest request, CancellationToken cancellationToken);
    Task<ComplianceTemplateDetailDto> UpdateAsync(Guid complianceTemplateId, SaveComplianceTemplateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid complianceTemplateId, CancellationToken cancellationToken);
}
