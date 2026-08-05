namespace ERMS.Domain.Enums;

/// <summary>
/// Talep yaşam döngüsü durumları (Bölüm 4.4 — Durum Diyagramı).
/// </summary>
public enum RequestStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
