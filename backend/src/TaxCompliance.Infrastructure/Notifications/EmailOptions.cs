namespace TaxCompliance.Infrastructure.Notifications;

public class EmailOptions
{
    public string Provider { get; set; } = "Development";
    public string FromEmail { get; set; } = "no-reply@taxplatform.local";
    public string FromName { get; set; } = "Tax Compliance Platform";
    public SmtpEmailOptions Smtp { get; set; } = new();
}

public class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
