using ERMS.Application.DTOs.Requests;

namespace ERMS.Application.Interfaces;

/// <summary>Bölüm 8.3 bonus (FR-40) — talebe dosya eki yükleme/indirme.</summary>
public interface IAttachmentService
{
    Task<AttachmentResponseDto> UploadAsync(
        int requestId,
        FileUploadDto file,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownloadResult> DownloadAsync(
        int requestId,
        int attachmentId,
        CancellationToken cancellationToken = default);
}
