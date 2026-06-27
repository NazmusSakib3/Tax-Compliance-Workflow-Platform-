using FluentAssertions;
using TaxCompliance.Infrastructure.Messaging;

namespace TaxCompliance.Tests.Messaging;

public class RabbitMqConnectionFactoryTests
{
    [Fact]
    public void Create_ShouldApplyConfiguredCredentials()
    {
        var options = new RabbitMqOptions
        {
            HostName = "rabbitmq.internal",
            UserName = "notifications",
            Password = "super-secret"
        };

        var factory = RabbitMqConnectionFactory.Create(options);

        factory.HostName.Should().Be("rabbitmq.internal");
        factory.UserName.Should().Be("notifications");
        factory.Password.Should().Be("super-secret");
    }
}
