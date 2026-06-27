using TaxCompliance.Application.Notifications;

namespace TaxCompliance.Infrastructure.Services;

public class NoOpTaskNotificationPublisher : ITaskNotificationPublisher
{
    public Task PublishAsync(TaskNotificationEvent notificationEvent, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
