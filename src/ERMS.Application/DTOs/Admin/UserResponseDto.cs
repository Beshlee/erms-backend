namespace ERMS.Application.DTOs.Admin;

public sealed record UserResponseDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    int DepartmentId,
    string DepartmentName,
    int? ManagerId,
    bool IsActive);
