namespace TaxCompliance.Infrastructure.FileStorage;

public class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "storage/uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".png", ".jpg", ".jpeg", ".txt"];
}

