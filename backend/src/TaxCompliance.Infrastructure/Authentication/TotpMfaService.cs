using OtpNet;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Infrastructure.Authentication;

public class TotpMfaService : IMfaService
{
    private const string Issuer = "TaxCompliancePlatform";

    public MfaSetupResponse GenerateSetup(string email, string? existingSecret)
    {
        var secret = existingSecret ?? GenerateSecret();
        var bytes = Base32Encoding.ToBytes(secret);
        var uri = new OtpUri(OtpType.Totp, bytes, email, Issuer).ToString();

        return new MfaSetupResponse
        {
            SharedKey = secret,
            AuthenticatorUri = uri
        };
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var bytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(bytes);
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }
}
