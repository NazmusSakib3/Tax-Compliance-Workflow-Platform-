using System.ComponentModel.DataAnnotations;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Application.ComplianceTaskRules;

public class ComplianceTaskRuleListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LegalEntityName { get; set; } = string.Empty;
    public string JurisdictionName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public RecurrenceType RecurrenceType { get; set; }
    public int DueDayOfMonth { get; set; }
    public int? DueMonthOfYear { get; set; }
    public bool IsActive { get; set; }
}

public class ComplianceTaskRuleDetailDto
{
    public Guid Id { get; set; }
    public Guid LegalEntityId { get; set; }
    public Guid JurisdictionId { get; set; }
    public Guid ComplianceTemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LegalEntityName { get; set; } = string.Empty;
    public string JurisdictionName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public RecurrenceType RecurrenceType { get; set; }
    public int DueDayOfMonth { get; set; }
    public int? DueMonthOfYear { get; set; }
    public bool IsActive { get; set; }
}

public class SaveComplianceTaskRuleRequest
{
    [Required]
    public Guid LegalEntityId { get; set; }

    [Required]
    public Guid JurisdictionId { get; set; }

    [Required]
    public Guid ComplianceTemplateId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public RecurrenceType RecurrenceType { get; set; }

    [Range(1, 31)]
    public int DueDayOfMonth { get; set; }

    [Range(1, 12)]
    public int? DueMonthOfYear { get; set; }

    public bool IsActive { get; set; } = true;
}

