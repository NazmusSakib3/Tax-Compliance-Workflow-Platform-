using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskRules;

namespace TaxCompliance.Application.ComplianceTaskRules;

public interface IComplianceTaskRuleService
{
    Task<PagedResult<ComplianceTaskRuleListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<ComplianceTaskRuleDetailDto> GetByIdAsync(Guid complianceTaskRuleId, CancellationToken cancellationToken);
    Task<ComplianceTaskRuleDetailDto> CreateAsync(SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken);
    Task<ComplianceTaskRuleDetailDto> UpdateAsync(Guid complianceTaskRuleId, SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid complianceTaskRuleId, CancellationToken cancellationToken);
}
