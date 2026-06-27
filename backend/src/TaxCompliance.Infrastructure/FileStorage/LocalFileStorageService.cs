using TaxCompliance.Application.FileStorage;

namespace TaxCompliance.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly LocalFileStorageOptions options;

    public LocalFileStorageService(LocalFileStorageOptions options)
    {
        this.options = options;
    }

    public async Task<string> SaveAsync(Stream stream, string originalFileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.RootPath);
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(options.RootPath, storedFileName);

        await using var outputStream = File.Create(absolutePath);
        await stream.CopyToAsync(outputStream, cancellationToken);
        return storedFileName;
    }

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(options.RootPath, storedPath);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }
}

