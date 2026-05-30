namespace MyWebPagesDownloader.Application.Options;

public sealed class StorageOptions
{
    public string OutputPath { get; set; } = "./downloads";
    public bool CompressContent { get; set; }
}
