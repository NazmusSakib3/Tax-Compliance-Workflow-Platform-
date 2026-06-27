namespace TaxCompliance.Application.FileStorage;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string originalFileName, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken);
}

