using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers;

/// <summary>
/// GET /api/request-types — herhangi bir giriş yapmış kullanıcı erişebilir (Bölüm 5.1).
/// Admin CRUD'u için bkz. Controllers/Admin/RequestTypesController.
/// </summary>
[ApiController]
[Route("api/request-types")]
[Authorize]
public sealed class RequestTypesController : ControllerBase
{
    private readonly IRequestTypeService _requestTypeService;

    public RequestTypesController(IRequestTypeService requestTypeService)
    {
        _requestTypeService = requestTypeService;
    }

    /// <summary>FR-15 — yalnızca aktif talep türlerini döner (ör. "Yeni Talep" formundaki tür seçimi için).</summary>
    [HttpGet]
    public async Task<ActionResult<List<RequestTypeResponseDto>>> GetActive(CancellationToken cancellationToken)
    {
        var result = await _requestTypeService.GetActiveAsync(cancellationToken);

        return Ok(result);
    }
}
