namespace ERMS.Domain.Enums;

/// <summary>
/// Sistemdeki kullanıcı rolleri (FR-05) — yetkilendirme kararları (kim neyi görebilir/yapabilir)
/// bu rol üzerinden verilir; bkz. controller'lardaki <c>[Authorize(Roles = ...)]</c> öznitelikleri.
/// </summary>
public enum UserRole
{
    /// <summary>Kendi taleplerini oluşturur/görür; yalnızca kendi verisine erişebilir.</summary>
    Employee = 0,

    /// <summary>Employee'nin yapabildiklerine ek olarak, kendisine bağlı (ManagerId) personelin taleplerini onaylar/reddeder.</summary>
    Manager = 1,

    /// <summary>Talep akışına katılmaz; departman/talep türü/kullanıcı yönetimi ve raporlarla ilgilenir.</summary>
    Admin = 2
}
