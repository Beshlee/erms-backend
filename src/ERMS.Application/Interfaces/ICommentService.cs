using ERMS.Application.DTOs.Requests;

namespace ERMS.Application.Interfaces;

/// <summary>FR-38, FR-39 — talep yorumları.</summary>
public interface ICommentService
{
    Task<CommentResponseDto> AddCommentAsync(
        int requestId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default);
}
