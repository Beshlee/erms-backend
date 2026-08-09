using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers.Admin;

/// <summary>FR-13, FR-14, FR-15 — admin talep türü yönetimi.</summary>
[ApiController]
[Route("api/admin/request-types")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class RequestTypesController : ControllerBase
{
    private readonly IRequestTypeService _requestTypeService;

    public RequestTypesController(IRequestTypeService requestTypeService)
    {
        _requestTypeService = requestTypeService;
    }

    /// <summary>Admin görünümü — pasif türler dahil hepsi (halka açık liste yalnızca aktifleri gösterir).</summary>
    [HttpGet]
    public async Task<ActionResult<List<RequestTypeResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _requestTypeService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RequestTypeResponseDto>> Create(
        CreateRequestTypeDto request,
        CancellationToken cancellationToken)
    {
        var result = await _requestTypeService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Güncelleme; IsActive=false gönderilerek pasife alma (soft-delete, FR-12/FR-15) da buradan yapılır.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RequestTypeResponseDto>> Update(
        int id,
        UpdateRequestTypeDto request,
        CancellationToken cancellationToken)
    {
        var result = await _requestTypeService.UpdateAsync(id, request, cancellationToken);

        return Ok(result);
    }
}
