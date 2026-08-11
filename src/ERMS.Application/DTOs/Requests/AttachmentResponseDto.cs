namespace ERMS.Application.DTOs.Requests;

/// <summary>Bölüm 8.3 bonus (FR-40) — talebe eklenen bir dosyanın yanıt gövdesi.</summary>
public sealed record AttachmentResponseDto(
    int Id,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt);
