using Microsoft.AspNetCore.Identity;

namespace TaxCompliance.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string? TotpSecret { get; set; }
    public bool IsMfaEnabled { get; set; }
}
