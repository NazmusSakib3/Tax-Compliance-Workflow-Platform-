using TaxCompliance.Infrastructure.Messaging;

namespace TaxCompliance.Infrastructure.Configuration;

public static class RabbitMqConnectionStringBuilder
{
    public static string? BuildAmqpUri(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            return null;
        }

        var userName = Uri.EscapeDataString(options.UserName);
        var password = Uri.EscapeDataString(options.Password);
        return $"amqp://{userName}:{password}@{options.HostName}:5672/";
    }
}
