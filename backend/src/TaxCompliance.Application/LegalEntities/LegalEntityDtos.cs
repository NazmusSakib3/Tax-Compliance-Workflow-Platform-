using System.ComponentModel.DataAnnotations;

namespace TaxCompliance.Application.LegalEntities;

public class LegalEntityListItemDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class LegalEntityDetailDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SaveLegalEntityRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string TaxIdentifier { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

