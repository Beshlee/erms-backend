using ERMS.Domain.Common;
using ERMS.Domain.Enums;

namespace ERMS.Domain.Entities;

/// <summary>Sisteme giriş yapabilen herkes — Employee, Manager ve Admin AYNI User tablosunda,
/// yalnızca <see cref="Role"/> alanıyla ayırt edilir (ayrı tablolara bölünmez).</summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    /// <summary>Düz metin parola ASLA saklanmaz — yalnızca BCrypt ile hash'lenmiş hâli (bkz. PasswordHasher).</summary>
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>
    /// Kendi kendine referans veren (self-referencing) ilişki: bir User'ın yöneticisi de bir
    /// User'dır. Employee/Manager'da doludur, en tepedeki yönetici veya Admin'de null olabilir.
    /// FR-32'deki "yönetici yalnızca kendisine bağlı personeli onaylayabilir" kuralı bu alana dayanır.
    /// </summary>
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    /// <summary>Bu kişinin yöneticisi olduğu kullanıcılar — ManagerId ilişkisinin ters yönü.</summary>
    public ICollection<User> Employees { get; set; } = [];

    /// <summary>Pasif kullanıcı giriş yapamaz (FR-08) — hesap silinmez, yalnızca devre dışı bırakılır.</summary>
    public bool IsActive { get; set; } = true;

    // Bölüm 8.3 bonus — refresh token akışı. Basitlik için kullanıcı başına TEK aktif
    // refresh token saklanır (yeniden giriş ya da yenileme bir öncekini geçersiz kılar);
    // çoklu cihaz/oturum desteği bu kapsamın dışında bırakılan bilinçli bir sınırlama.
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public ICollection<Request> Requests { get; set; } = [];
    public ICollection<Approval> Approvals { get; set; } = [];
    public ICollection<RequestComment> Comments { get; set; } = [];
}
