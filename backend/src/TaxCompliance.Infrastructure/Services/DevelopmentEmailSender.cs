using Microsoft.Extensions.Logging;
using TaxCompliance.Application.Notifications;

namespace TaxCompliance.Infrastructure.Services;

public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        this.logger = logger;
    }

    public Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Development email sender invoked. To: {RecipientEmail}, Subject: {Subject}, Body: {Body}",
            recipientEmail,
            subject,
            RedactSensitiveEmailContent(body));

        return Task.CompletedTask;
    }

    private static string RedactSensitiveEmailContent(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        return System.Text.RegularExpressions.Regex.Replace(
            body,
            @"token=[^&\s]+",
            "token=[redacted]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

