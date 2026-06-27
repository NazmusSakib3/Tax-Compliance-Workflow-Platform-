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
        // Only the non-sensitive subject is logged. The recipient address (PII) and the
        // message body (which can contain password-reset tokens) are deliberately not
        // logged to avoid clear-text logging of sensitive information.
        logger.LogInformation(
            "Development email sender invoked. Subject: {Subject}",
            subject);

        return Task.CompletedTask;
    }
}

