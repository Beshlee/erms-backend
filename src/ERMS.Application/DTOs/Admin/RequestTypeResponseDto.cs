namespace ERMS.Application.DTOs.Admin;

public sealed record RequestTypeResponseDto(
    int Id,
    string Name,
    string? Description,
    bool RequiresApproval,
    bool IsActive);
