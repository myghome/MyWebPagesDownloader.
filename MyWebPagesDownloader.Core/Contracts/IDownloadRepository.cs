using MyWebPagesDownloader.Core.Entities;
using MyWebPagesDownloader.Core.Enums;

namespace MyWebPagesDownloader.Core.Contracts;

public interface IDownloadRepository
{
    Task<DownloadRecord?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DownloadRecord>> GetFailedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DownloadRecord>> GetRecentAsync(int count, CancellationToken cancellationToken);
    Task<IReadOnlyList<DownloadRecord>> GetByStatusAsync(DownloadStatus status, CancellationToken cancellationToken);
    Task<IReadOnlyList<DownloadRecord>> GetIncompleteAsync(CancellationToken cancellationToken);
    Task SaveAsync(DownloadRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(DownloadRecord record, CancellationToken cancellationToken);
}
