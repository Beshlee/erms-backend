namespace ERMS.Application.DTOs.Requests;

/// <summary>
/// Api katmanı, ASP.NET Core'a özgü <c>IFormFile</c>'ı Application'a sızdırmamak için
/// multipart isteği bu sade sözleşmeye çevirip geçirir (katman bağımsızlığı — bkz. proje
/// mimari notları, ICurrentUserService'in Api'de kalmasıyla aynı gerekçe).
/// </summary>
public sealed class FileUploadDto
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
}
