namespace TaxCompliance.Infrastructure.Notifications;

public class NotificationProcessingOptions
{
    public int DueSoonDays { get; set; } = 7;
    public int ScanIntervalMinutes { get; set; } = 30;
    public string DefaultRecipientEmail { get; set; } = "admin@taxplatform.local";
}

