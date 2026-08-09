namespace ERMS.Application.DTOs.Requests;

/// <summary>FR-39 — yorum, yazan kişi ve oluşturulma zamanıyla birlikte gösterilir.</summary>
public sealed record CommentResponseDto(
    int Id,
    string Content,
    string AuthorName,
    DateTime CreatedAt);
