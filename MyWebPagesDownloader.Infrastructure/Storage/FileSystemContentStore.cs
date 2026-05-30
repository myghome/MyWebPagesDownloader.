using MyWebPagesDownloader.Core.Contracts;

namespace MyWebPagesDownloader.Infrastructure.Storage;

public sealed class FileSystemContentStore : IContentStore
{
    private readonly string _basePath;

    public FileSystemContentStore(string basePath = "./downloads")
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(string url, string content, CancellationToken cancellationToken)
    {
        var filename = GenerateFilename(url);
        var filepath = Path.Combine(_basePath, filename);
        await File.WriteAllTextAsync(filepath, content, cancellationToken);
        return filepath;
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    private static string GenerateFilename(string url)
    {
        var hash = url.GetHashCode().ToString("X");
        return $"{hash}.html";
    }
}
