using ERMS.Domain.Enums;

namespace ERMS.Application.DTOs.Admin;

/// <summary>
/// FR-07/FR-12 — admin kullanıcıyı günceller/pasife alır. Parola değişikliği kapsam dışı
/// (bonus) — burada yok. IsActive=false, fiziksel silme yerine soft-delete'i sağlar.
/// </summary>
public sealed record UpdateUserDto(
    string FirstName,
    string LastName,
    UserRole Role,
    int DepartmentId,
    int? ManagerId,
    bool IsActive);
