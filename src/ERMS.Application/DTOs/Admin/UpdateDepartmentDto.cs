namespace ERMS.Application.DTOs.Admin;

/// <summary>FR-11/FR-12 — departmanı günceller/pasife alır (soft-delete).</summary>
public sealed record UpdateDepartmentDto(string Name, bool IsActive);
