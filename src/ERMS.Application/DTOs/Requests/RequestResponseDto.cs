namespace ERMS.Application.DTOs.Requests;

/// <summary>Bölüm 5.3 — POST /api/requests 201 Created yanıt gövdesi.</summary>
public sealed record RequestResponseDto(
    int Id,
    string Title,
    string Type,
    string Status,
    DateTime CreatedAt);
