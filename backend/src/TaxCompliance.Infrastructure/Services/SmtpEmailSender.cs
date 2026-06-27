using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.Notifications;
using TaxCompliance.Infrastructure.Notifications;

namespace TaxCompliance.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailDeliveryOptions options;

    public SmtpEmailSender(IOptions<EmailDeliveryOptions> options)
    {
        this.options = options.Value;
    }

    public async Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost must be configured when Email:Provider is Smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("Email:FromAddress must be configured when Email:Provider is Smtp.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            client.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
