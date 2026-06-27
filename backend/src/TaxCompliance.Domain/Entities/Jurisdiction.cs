using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class Jurisdiction : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string FilingAuthority { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ComplianceTaskRule> ComplianceTaskRules { get; set; } = new List<ComplianceTaskRule>();
}

