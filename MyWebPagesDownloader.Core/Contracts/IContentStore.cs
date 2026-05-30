namespace MyWebPagesDownloader.Core.Contracts;

public interface IContentStore
{
    Task<string> SaveAsync(
        string url,
        string content,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string filePath,
        CancellationToken cancellationToken);
}
