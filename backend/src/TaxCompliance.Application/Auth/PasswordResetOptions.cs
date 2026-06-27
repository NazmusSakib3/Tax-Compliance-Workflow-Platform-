namespace TaxCompliance.Application.Auth;

public class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public string ClientResetUrl { get; set; } = "http://localhost:4200/reset-password";
}
