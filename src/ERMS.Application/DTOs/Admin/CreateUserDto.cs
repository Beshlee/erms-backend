using ERMS.Domain.Enums;

namespace ERMS.Application.DTOs.Admin;

/// <summary>FR-07 — admin yeni kullanıcı oluşturur.</summary>
public sealed record CreateUserDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role,
    int DepartmentId,
    int? ManagerId);
