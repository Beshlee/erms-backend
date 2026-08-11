using ERMS.Domain.Common;

namespace ERMS.Domain.Entities;

/// <summary>Talep dosya ekleri (FR-40 — altyapı çekirdek, yükleme bonus).</summary>
public class RequestAttachment : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    /// <summary>Kullanıcının yüklediği ORİJİNAL dosya adı (indirirken bu adla sunulur).</summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Diskteki GERÇEK konum — <see cref="FileName"/> ile AYNI değildir. Ad çakışmasını ve
    /// path traversal riskini önlemek için dosya diske rastgele bir adla yazılır (bkz.
    /// Infrastructure/Storage/LocalFileStorageService.SaveAsync); bu alan o rastgele anahtarı
    /// tutar, örn. "5007/a1b2c3....pdf".
    /// </summary>
    public string FilePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }
}
