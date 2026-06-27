using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Notifications;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.BackgroundServices;

public class TaskNotificationMonitorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly NotificationProcessingOptions options;
    private readonly ILogger<TaskNotificationMonitorHostedService> logger;

    public TaskNotificationMonitorHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<NotificationProcessingOptions> options,
        ILogger<TaskNotificationMonitorHostedService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationPublisher = scope.ServiceProvider.GetRequiredService<ITaskNotificationPublisher>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dueSoonLimit = today.AddDays(options.DueSoonDays);

            var candidates = await dbContext.ComplianceTaskOccurrences
                .AsNoTracking()
                .Include(occurrence => occurrence.ComplianceTaskRule)
                .Where(occurrence =>
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Completed &&
                    occurrence.Status != ComplianceTaskOccurrenceStatus.Cancelled &&
                    (occurrence.DueDate < today || occurrence.DueDate <= dueSoonLimit))
                .ToListAsync(stoppingToken);

            if (candidates.Count == 0)
            {
                logger.LogInformation("Task notification monitor completed at {RunTimeUtc}", DateTime.UtcNow);
                await Task.Delay(TimeSpan.FromMinutes(options.ScanIntervalMinutes), stoppingToken);
                continue;
            }

            var occurrenceIds = candidates.Select(occurrence => occurrence.Id).ToArray();
            var existingNotifications = await dbContext.NotificationMessages
                .AsNoTracking()
                .Where(message => occurrenceIds.Contains(message.ComplianceTaskOccurrenceId))
                .Select(message => new { message.ComplianceTaskOccurrenceId, message.NotificationType })
                .ToListAsync(stoppingToken);

            var existingNotificationKeys = existingNotifications
                .Select(notification => (notification.ComplianceTaskOccurrenceId, notification.NotificationType))
                .ToHashSet();

            var assignedUserIds = candidates
                .Where(occurrence => !string.IsNullOrWhiteSpace(occurrence.AssignedToUserId))
                .Select(occurrence => occurrence.AssignedToUserId!)
                .Distinct()
                .ToArray();

            var recipientEmailsByUserId = assignedUserIds.Length == 0
                ? new Dictionary<string, string?>()
                : await userManager.Users
                    .AsNoTracking()
                    .Where(user => assignedUserIds.Contains(user.Id))
                    .Select(user => new { user.Id, user.Email })
                    .ToDictionaryAsync(user => user.Id, user => user.Email, stoppingToken);

            var newMessages = new List<NotificationMessage>();

            foreach (var occurrence in candidates)
            {
                var notificationType = occurrence.DueDate < today
                    ? NotificationTypes.Overdue
                    : NotificationTypes.DueSoon;

                if (existingNotificationKeys.Contains((occurrence.Id, notificationType)))
                {
                    continue;
                }

                var recipientEmail = !string.IsNullOrWhiteSpace(occurrence.AssignedToUserId) &&
                                     recipientEmailsByUserId.TryGetValue(occurrence.AssignedToUserId, out var email)
                    ? email ?? options.DefaultRecipientEmail
                    : options.DefaultRecipientEmail;

                var subject = notificationType == NotificationTypes.Overdue
                    ? $"Overdue task: {occurrence.ComplianceTaskRule?.Title}"
                    : $"Due soon task: {occurrence.ComplianceTaskRule?.Title}";
                var body = notificationType == NotificationTypes.Overdue
                    ? $"Task '{occurrence.ComplianceTaskRule?.Title}' is overdue as of {occurrence.DueDate:yyyy-MM-dd}."
                    : $"Task '{occurrence.ComplianceTaskRule?.Title}' is due soon on {occurrence.DueDate:yyyy-MM-dd}.";

                newMessages.Add(new NotificationMessage
                {
                    ComplianceTaskOccurrenceId = occurrence.Id,
                    NotificationType = notificationType,
                    RecipientEmail = recipientEmail,
                    Subject = subject,
                    Body = body,
                    IsProcessed = false
                });
            }

            if (newMessages.Count > 0)
            {
                dbContext.NotificationMessages.AddRange(newMessages);
                await dbContext.SaveChangesAsync(stoppingToken);

                foreach (var notificationMessage in newMessages)
                {
                    await notificationPublisher.PublishAsync(
                        new TaskNotificationEvent { NotificationMessageId = notificationMessage.Id },
                        stoppingToken);
                }
            }

            logger.LogInformation("Task notification monitor completed at {RunTimeUtc}", DateTime.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(options.ScanIntervalMinutes), stoppingToken);
        }
    }
}
