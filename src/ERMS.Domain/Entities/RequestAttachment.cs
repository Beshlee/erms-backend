using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

/// <summary>Talep dosya ekleri (FR-40 — altyapı çekirdek, yükleme bonus).</summary>
public class RequestAttachment : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }
}
