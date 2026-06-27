using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TaxCompliance.Api.Configuration;
using TaxCompliance.Infrastructure.Authentication;

namespace TaxCompliance.Tests.Configuration;

public class ProductionConfigurationValidatorTests
{
    [Fact]
    public void Validate_ShouldThrowInProductionWhenRequiredSettingsAreMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var jwtOptions = new JwtOptions();

        var act = () => ProductionConfigurationValidator.Validate(configuration, jwtOptions, Environments.Production);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:DefaultConnection*");
    }

    [Fact]
    public void Validate_ShouldRequireCorsOriginsInProduction()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=postgres;Database=taxcompliance;Username=app;Password=secret",
                ["ConnectionStrings:Redis"] = "redis:6379,password=secret",
                ["Seed:AdminEmail"] = "admin@example.com",
                ["Seed:AdminPassword"] = "Admin123!",
                ["Notifications:DefaultRecipientEmail"] = "ops@example.com",
                ["PasswordReset:ClientResetUrl"] = "https://app.example.com/reset-password",
                ["RabbitMq:HostName"] = "rabbitmq",
                ["RabbitMq:UserName"] = "notifications",
                ["RabbitMq:Password"] = "secret",
                ["Email:FromAddress"] = "no-reply@example.com",
                ["Email:SmtpHost"] = "smtp.example.com",
                ["Email:Username"] = "smtp-user",
                ["Email:Password"] = "smtp-secret"
            })
            .Build();
        var jwtOptions = new JwtOptions
        {
            SigningKey = "replace-with-at-least-32-random-characters"
        };

        var act = () => ProductionConfigurationValidator.Validate(configuration, jwtOptions, Environments.Production);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cors:AllowedOrigins*");
    }

    [Fact]
    public void Validate_ShouldAcceptCompleteProductionConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=postgres;Database=taxcompliance;Username=app;Password=secret",
                ["ConnectionStrings:Redis"] = "redis:6379,password=secret",
                ["Seed:AdminEmail"] = "admin@example.com",
                ["Seed:AdminPassword"] = "Admin123!",
                ["Notifications:DefaultRecipientEmail"] = "ops@example.com",
                ["PasswordReset:ClientResetUrl"] = "https://app.example.com/reset-password",
                ["RabbitMq:HostName"] = "rabbitmq",
                ["RabbitMq:UserName"] = "notifications",
                ["RabbitMq:Password"] = "secret",
                ["Email:FromAddress"] = "no-reply@example.com",
                ["Email:SmtpHost"] = "smtp.example.com",
                ["Email:Username"] = "smtp-user",
                ["Email:Password"] = "smtp-secret",
                ["Cors:AllowedOrigins:0"] = "https://app.example.com"
            })
            .Build();
        var jwtOptions = new JwtOptions
        {
            SigningKey = "replace-with-at-least-32-random-characters"
        };

        var act = () => ProductionConfigurationValidator.Validate(configuration, jwtOptions, Environments.Production);

        act.Should().NotThrow();
    }
}
