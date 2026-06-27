using TaxCompliance.Domain.Common;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Domain.Entities;

public class ComplianceTaskRule : BaseEntity
{
    public Guid LegalEntityId { get; set; }
    public Guid JurisdictionId { get; set; }
    public Guid ComplianceTemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecurrenceType RecurrenceType { get; set; }
    public int DueDayOfMonth { get; set; }
    public int? DueMonthOfYear { get; set; }
    public bool IsActive { get; set; } = true;
    public LegalEntity? LegalEntity { get; set; }
    public Jurisdiction? Jurisdiction { get; set; }
    public ComplianceTemplate? ComplianceTemplate { get; set; }
}

