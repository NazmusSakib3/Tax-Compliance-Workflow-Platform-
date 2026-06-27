namespace TaxCompliance.Application.Notifications;

public interface IEmailSender
{
    Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken);
}
