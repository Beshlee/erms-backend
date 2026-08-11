namespace ERMS.Application.DTOs.Requests;

/// <summary>İndirme uç noktasının Api katmanına döndürdüğü akış + dosya bilgisi.</summary>
public sealed class AttachmentDownloadResult
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
