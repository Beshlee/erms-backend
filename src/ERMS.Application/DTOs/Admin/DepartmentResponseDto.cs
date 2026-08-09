namespace ERMS.Application.DTOs.Admin;

public sealed record DepartmentResponseDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt);
