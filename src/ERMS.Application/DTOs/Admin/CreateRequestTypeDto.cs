namespace ERMS.Application.DTOs.Admin;

/// <summary>FR-13/FR-14 — admin yeni talep türü tanımlar.</summary>
public sealed record CreateRequestTypeDto(
    string Name,
    string? Description,
    bool RequiresApproval);
