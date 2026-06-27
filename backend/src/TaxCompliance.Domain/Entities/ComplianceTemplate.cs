using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class ComplianceTemplate : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilingType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReminderDaysBeforeDue { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ComplianceTaskRule> ComplianceTaskRules { get; set; } = new List<ComplianceTaskRule>();
}

