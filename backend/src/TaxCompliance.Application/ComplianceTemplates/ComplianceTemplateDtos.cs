using System.ComponentModel.DataAnnotations;

namespace TaxCompliance.Application.ComplianceTemplates;

public class ComplianceTemplateListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilingType { get; set; } = string.Empty;
    public int ReminderDaysBeforeDue { get; set; }
    public bool IsActive { get; set; }
}

public class ComplianceTemplateDetailDto : ComplianceTemplateListItemDto
{
    public string Description { get; set; } = string.Empty;
}

public class SaveComplianceTemplateRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FilingType { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 365)]
    public int ReminderDaysBeforeDue { get; set; }

    public bool IsActive { get; set; } = true;
}

