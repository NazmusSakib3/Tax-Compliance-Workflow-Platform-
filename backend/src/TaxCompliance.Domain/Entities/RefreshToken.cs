using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public bool IsActive => RevokedUtc is null && ExpiresUtc > DateTime.UtcNow;
}
