namespace TaxCompliance.Api.Configuration;

public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool EnableHsts { get; set; }

    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;

    public bool HstsIncludeSubDomains { get; set; } = true;
}
