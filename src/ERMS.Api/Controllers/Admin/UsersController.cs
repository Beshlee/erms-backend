using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Api.Controllers.Admin;

/// <summary>FR-07..10, FR-12 — admin kullanıcı yönetimi.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Create(
        CreateUserDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Güncelleme; IsActive=false gönderilerek pasife alma (soft-delete, FR-12) da buradan yapılır.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> Update(
        int id,
        UpdateUserDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateAsync(id, request, cancellationToken);

        return Ok(result);
    }
}
