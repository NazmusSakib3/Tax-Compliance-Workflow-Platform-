using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class LegalEntity : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Organization? Organization { get; set; }
    public ICollection<ComplianceTaskRule> ComplianceTaskRules { get; set; } = new List<ComplianceTaskRule>();
}

