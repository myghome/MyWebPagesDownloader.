namespace MyWebPagesDownloader.Core.Enums;

public enum DuplicateUrlStrategy
{
    RejectDuplicate = 0,
    AllowAfterMinutes = 1,
    AllowIfPreviousFailed = 2
}
