using FluentAssertions;
using Microsoft.Extensions.Options;
using TaxCompliance.Infrastructure.Notifications;
using TaxCompliance.Infrastructure.Services;

namespace TaxCompliance.Tests.Services;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_ShouldThrowWhenSmtpHostIsMissing()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailDeliveryOptions
        {
            FromAddress = "no-reply@example.com"
        }));

        var act = () => sender.SendAsync("user@example.com", "Subject", "Body", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email:SmtpHost*");
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenFromAddressIsMissing()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailDeliveryOptions
        {
            SmtpHost = "smtp.example.com",
            FromAddress = string.Empty
        }));

        var act = () => sender.SendAsync("user@example.com", "Subject", "Body", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email:FromAddress*");
    }
}
