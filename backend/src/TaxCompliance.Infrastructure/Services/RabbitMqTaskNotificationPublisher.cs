using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Infrastructure.Messaging;

namespace TaxCompliance.Infrastructure.Services;

public class RabbitMqTaskNotificationPublisher : ITaskNotificationPublisher
{
    private readonly RabbitMqOptions options;

    public RabbitMqTaskNotificationPublisher(RabbitMqOptions options)
    {
        this.options = options;
    }

    public async Task PublishAsync(TaskNotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        var factory = RabbitMqConnectionFactory.Create(options);
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notificationEvent));
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: options.QueueName,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: body,
            cancellationToken: cancellationToken);
    }
}
