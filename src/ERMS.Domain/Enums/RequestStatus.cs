namespace ERMS.Domain.Enums;

/// <summary>
/// Talep yaşam döngüsü durumları (Bölüm 4.4 — Durum Diyagramı). Olağan akış:
/// <c>Draft → Pending → (Approved | Rejected)</c>; Pending'ken sahibi <c>Cancelled</c>'a da
/// çekebilir (FR-30). Bir talep sonuçlandıktan (Approved/Rejected/Cancelled) sonra bir daha
/// değiştirilemez — bunu uygulayan kod için bkz. RequestService/ApprovalService.
/// </summary>
public enum RequestStatus
{
    /// <summary>Henüz gönderilmemiş, sahibi tarafından hâlâ düzenlenebilen taslak (FR-21).</summary>
    Draft = 0,

    /// <summary>Gönderilmiş, yöneticinin karar vermesini bekleyen talep.</summary>
    Pending = 1,

    /// <summary>Yönetici onayladı (ya da tür onay gerektirmiyorsa doğrudan bu duruma geçti, FR-24).</summary>
    Approved = 2,

    /// <summary>Yönetici reddetti — gerekçe zorunludur (FR-34).</summary>
    Rejected = 3,

    /// <summary>Sahibi tarafından, henüz karara bağlanmadan iptal edildi (yalnızca Pending'ken mümkün).</summary>
    Cancelled = 4
}
