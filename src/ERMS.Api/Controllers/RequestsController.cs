using ERMS.Application.Common.Models;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Manager)}")]
public sealed class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
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
    /// FR-25..29 — giriş yapan kullanıcının kendi taleplerini durum/tür/başlığa göre
    /// filtreleyip sayfalı listeler. Örnek: /api/requests?status=Pending&amp;typeId=1&amp;search=izin&amp;page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestResponseDto>>> GetMyRequests(
        [FromQuery] string? status,
        [FromQuery] int? typeId,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        // page/pageSize belirtilmezse (0 gelirse) Application katmanı FR-28 için makul
        // varsayılanlara (page=1, pageSize=10) çeker.
        var result = await _requestService.GetMyRequestsAsync(
            status, typeId, search, page, pageSize, cancellationToken);

        return Ok(result);
    }
}
