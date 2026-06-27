namespace TaxCompliance.Infrastructure.Notifications;

public class EmailDeliveryOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Development";
    public string FromAddress { get; set; } = "no-reply@taxplatform.local";
    public string FromName { get; set; } = "Tax Compliance Workflow Platform";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
