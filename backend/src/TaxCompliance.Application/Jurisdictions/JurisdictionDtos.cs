using System.ComponentModel.DataAnnotations;

namespace TaxCompliance.Application.Jurisdictions;

public class JurisdictionListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string FilingAuthority { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class JurisdictionDetailDto : JurisdictionListItemDto
{
}

public class SaveJurisdictionRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string RegionCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FilingAuthority { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

