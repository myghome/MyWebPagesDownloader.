using MyWebPagesDownloader.Core.Exceptions;

namespace MyWebPagesDownloader.Application.Validation;

public sealed class UrlValidator
{
    private static readonly HashSet<string> SeenUrls = new(StringComparer.OrdinalIgnoreCase);

    public void Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ValidationException("URL cannot be empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ValidationException($"Invalid URL format: {url}");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException($"Only HTTP/HTTPS URLs are supported. Got: {uri.Scheme}");
    }

    public bool IsUrlDuplicate(string url) => !SeenUrls.Add(url);

    public void ClearSeenUrls() => SeenUrls.Clear();
}
