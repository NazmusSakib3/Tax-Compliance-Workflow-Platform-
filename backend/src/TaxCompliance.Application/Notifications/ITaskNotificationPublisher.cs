namespace TaxCompliance.Application.Notifications;

public interface ITaskNotificationPublisher
{
    Task PublishAsync(TaskNotificationEvent notificationEvent, CancellationToken cancellationToken);
}

