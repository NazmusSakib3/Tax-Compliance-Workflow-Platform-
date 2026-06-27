using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<LegalEntity> LegalEntities { get; set; } = new List<LegalEntity>();
}

