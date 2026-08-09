namespace ERMS.Application.DTOs.Requests;

/// <summary>POST /api/requests/{id}/comments — FR-38.</summary>
public sealed record CreateCommentDto(string Content);
