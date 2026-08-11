using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

/// <summary>
/// Durum değişikliği geçmişi / audit log (FR-41, FR-42) — bir Request'in yaşamı boyunca
/// geçirdiği HER durum değişikliğinde (oluşturma, gönderme, onay, red, iptal) bir kayıt
/// eklenir (bkz. RequestService/ApprovalService.AddHistoryAsync). Silinmez, yalnızca eklenir
/// — "kim, ne zaman, hangi durumdan hangi duruma geçirdi" sorusuna cevap verir.
/// </summary>
public class RequestHistory : BaseEntity
{
    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    /// <summary>Bu değişikliği tetikleyen kişi — talep sahibi (gönder/iptal) ya da yönetici (onay/red) olabilir.</summary>
    public int ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;

    public RequestStatus OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }

    /// <summary>Serbest metin açıklama (ör. "Talep onaylandı.") — Approval.Comment'ten farklı, kullanıcı girdisi değil, sistem tarafından üretilir.</summary>
    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; }
}
