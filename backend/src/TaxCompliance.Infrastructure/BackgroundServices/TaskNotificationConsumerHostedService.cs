using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Infrastructure.Messaging;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.BackgroundServices;

public class TaskNotificationConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly RabbitMqOptions options;
    private readonly ILogger<TaskNotificationConsumerHostedService> logger;

    public TaskNotificationConsumerHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<TaskNotificationConsumerHostedService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(options);
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var notificationEvent = JsonSerializer.Deserialize<TaskNotificationEvent>(payload);
                if (notificationEvent is null)
                {
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    return;
                }

                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var notificationMessage = await dbContext.NotificationMessages.FindAsync([notificationEvent.NotificationMessageId], cancellationToken: stoppingToken);
                if (notificationMessage is null)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                if (notificationMessage.IsProcessed)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                notificationMessage.IsProcessed = true;
                notificationMessage.ProcessedUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);

                try
                {
                    await emailSender.SendAsync(notificationMessage.RecipientEmail, notificationMessage.Subject, notificationMessage.Body, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "Email send failed after notification {NotificationMessageId} was marked processed. Message will not be requeued.",
                        notificationMessage.Id);
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    return;
                }

                logger.LogInformation(
                    "Notification processed for occurrence {OccurrenceId} with type {NotificationType}",
                    notificationMessage.ComplianceTaskOccurrenceId,
                    notificationMessage.NotificationType);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process notification message with delivery tag {DeliveryTag}.", eventArgs.DeliveryTag);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
