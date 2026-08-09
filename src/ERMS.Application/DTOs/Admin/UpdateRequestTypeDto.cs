namespace ERMS.Application.DTOs.Admin;

/// <summary>FR-13/FR-14/FR-15 — talep türünü günceller/pasife alır.</summary>
public sealed record UpdateRequestTypeDto(
    string Name,
    string? Description,
    bool RequiresApproval,
    bool IsActive);
