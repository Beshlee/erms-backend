using ERMS.Application.Common.Models;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers;

// [ApiController]: JSON model-binding/doğrulama gibi Web API'ye özgü davranışları açar.
// [Route("api/requests")]: bu sınıftaki tüm action'ların temel adresi — ör. aşağıdaki
// [HttpPost("{id:int}/submit")], gerçekte "POST /api/requests/{id}/submit" olur.
// [Authorize(Roles = ...)]: JWT içindeki "role" claim'i Employee veya Manager DEĞİLSE
// (ya da token hiç yoksa) istek controller'a hiç girmeden 401/403 ile reddedilir —
// bu kontrol Program.cs'teki "app.UseAuthorization()" middleware'inde yapılır.
/// <summary>
/// Talep CRUD'u, gönderme/iptal, yorumlar ve dosya ekleri — tek bir "kaynak" (Request) etrafında
/// toplandığı için hepsi aynı controller'da (REST'te alt-kaynaklar aynı route öneki altında
/// gruplanır: /api/requests/{id}/comments, /api/requests/{id}/attachments).
/// </summary>
[ApiController]
[Route("api/requests")]
[Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Manager)}")]
public sealed class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly ICommentService _commentService;
    private readonly IAttachmentService _attachmentService;

    public RequestsController(
        IRequestService requestService,
        ICommentService commentService,
        IAttachmentService attachmentService)
    {
        _requestService = requestService;
        _commentService = commentService;
        _attachmentService = attachmentService;
    }

    /// <summary>FR-16..21, FR-24 — yeni talep oluşturur (Draft/Pending/Approved).</summary>
    [HttpPost]
    public async Task<ActionResult<RequestResponseDto>> Create(
        CreateRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>FR-22 — yalnızca talep sahibi kendi taslak talebini günceller.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RequestResponseDto>> Update(
        int id,
        UpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.UpdateAsync(id, request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// FR-25..29 — giriş yapan kullanıcının kendi taleplerini durum/tür/arama/öncelik/tarih
    /// aralığı/tutar aralığına göre filtreleyip sayfalı listeler (priority..maxAmount —
    /// Bölüm 8.3 bonus: global arama + gelişmiş filtreleme). Örnek:
    /// /api/requests?status=Pending&amp;typeId=1&amp;search=izin&amp;priority=High&amp;createdFrom=2026-08-01&amp;minAmount=100&amp;page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestResponseDto>>> GetMyRequests(
        [FromQuery] string? status,
        [FromQuery] int? typeId,
        [FromQuery] string? search,
        [FromQuery] string? priority,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        // page/pageSize belirtilmezse (0 gelirse) Application katmanı FR-28 için makul
        // varsayılanlara (page=1, pageSize=10) çeker.
        var result = await _requestService.GetMyRequestsAsync(
            status, typeId, search, priority, createdFrom, createdTo, minAmount, maxAmount,
            page, pageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-42, US-07 — talep detayı ve kronolojik durum geçmişi.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestDetailDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.GetDetailAsync(id, cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-23/24 — taslağı gönderir (Draft → Pending/Approved).</summary>
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<RequestResponseDto>> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.SubmitAsync(id, cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-30 — bekleyen (Pending) talebi iptal eder.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<RequestResponseDto>> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.CancelAsync(id, cancellationToken);

        return Ok(result);
    }

    /// <summary>FR-38, FR-39 — talebe yorum ekler (talep sahibi ve ilgili yönetici).</summary>
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentResponseDto>> AddComment(
        int id,
        CreateCommentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _commentService.AddCommentAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Bölüm 8.3 bonus (FR-40) — talebe dosya eki yükler (multipart/form-data).
    /// Yalnızca talep sahibi, talep Taslak/Beklemede durumundayken yükleyebilir.
    /// </summary>
    [HttpPost("{id:int}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentResponseDto>> UploadAttachment(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var uploadDto = new FileUploadDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = stream
        };

        var result = await _attachmentService.UploadAsync(id, uploadDto, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Bölüm 8.3 bonus (FR-40) — talebe eklenmiş bir dosyayı indirir
    /// (talep sahibi ve ilgili yönetici, FR-26 ile aynı görünürlük kuralı).
    /// </summary>
    [HttpGet("{id:int}/attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadAttachment(
        int id,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await _attachmentService.DownloadAsync(id, attachmentId, cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }
}
