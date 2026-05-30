using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.ValueObjects;
using System.Net;

namespace MyWebPagesDownloader.Infrastructure.Http;

public sealed class HttpClientProvider : IDownloadProvider
{
    private readonly HttpClient _httpClient;

    public HttpClientProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<DownloadResult> DownloadAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            return new DownloadResult(
                Success: response.IsSuccessStatusCode,
                StatusCode: response.StatusCode,
                ContentLength: content.Length,
                Duration: duration,
                ErrorMessage: response.IsSuccessStatusCode ? null : response.StatusCode.ToString());
        }
        catch (HttpRequestException ex)
        {
            return new DownloadResult(
                Success: false,
                ErrorMessage: ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            return new DownloadResult(
                Success: false,
                ErrorMessage: "Request timeout: " + ex.Message);
        }
    }
}
