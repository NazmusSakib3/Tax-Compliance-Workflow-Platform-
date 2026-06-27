using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Infrastructure.Authentication;

public class DataProtectionMfaSecretProtector : IMfaSecretProtector
{
    private const string Prefix = "protected:";
    private readonly IDataProtector protector;

    public DataProtectionMfaSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider.CreateProtector("TaxCompliance.Mfa.TotpSecret.v1");
    }

    public string Protect(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return Prefix + protector.Protect(secret);
    }

    public string? Unprotect(string? storedSecret)
    {
        if (string.IsNullOrWhiteSpace(storedSecret))
        {
            return null;
        }

        if (!IsProtected(storedSecret))
        {
            return storedSecret;
        }

        try
        {
            return protector.Unprotect(storedSecret[Prefix.Length..]);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public bool IsProtected(string? storedSecret)
    {
        return storedSecret?.StartsWith(Prefix, StringComparison.Ordinal) == true;
    }
}
