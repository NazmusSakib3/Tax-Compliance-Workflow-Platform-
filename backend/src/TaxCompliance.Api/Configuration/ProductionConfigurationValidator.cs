using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TaxCompliance.Infrastructure.Authentication;

namespace TaxCompliance.Api.Configuration;

public static class ProductionConfigurationValidator
{
    private static readonly string[] RequiredProductionKeys =
    [
        "ConnectionStrings:DefaultConnection",
        "ConnectionStrings:Redis",
        "Seed:AdminEmail",
        "Seed:AdminPassword",
        "Notifications:DefaultRecipientEmail",
        "PasswordReset:ClientResetUrl",
        "RabbitMq:HostName",
        "RabbitMq:UserName",
        "RabbitMq:Password",
        "Email:FromAddress",
        "Email:SmtpHost",
        "Email:Username",
        "Email:Password",
        "PasswordReset:ClientResetUrl"
    ];

    public static void Validate(IConfiguration configuration, JwtOptions jwtOptions, string environmentName)
    {
        if (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var missingKeys = RequiredProductionKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            missingKeys = missingKeys
                .Prepend("Jwt:SigningKey (min 32 characters)")
                .ToArray();
        }

        if (!HasConfiguredCorsOrigins(configuration))
        {
            missingKeys = missingKeys
                .Append("Cors:AllowedOrigins (at least one origin)")
                .ToArray();
        }

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing required non-Development configuration: {string.Join(", ", missingKeys)}.");
        }
    }

    private static bool HasConfiguredCorsOrigins(IConfiguration configuration)
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        var configuredValue = configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            origins = origins
                .Concat(configuredValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();
        }

        return origins.Any(origin => !string.IsNullOrWhiteSpace(origin));
    }
}
