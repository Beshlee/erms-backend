using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers.Admin;

/// <summary>FR-11, FR-12 — admin departman yönetimi.</summary>
[ApiController]
[Route("api/admin/departments")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponseDto>> Create(
        CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Güncelleme; IsActive=false gönderilerek pasife alma (soft-delete, FR-12) da buradan yapılır.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponseDto>> Update(
        int id,
        UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.UpdateAsync(id, request, cancellationToken);

        return Ok(result);
    }
}
