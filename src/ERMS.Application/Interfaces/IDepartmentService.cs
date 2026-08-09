using ERMS.Application.DTOs.Admin;

namespace ERMS.Application.Interfaces;

/// <summary>FR-11, FR-12 — admin departman yönetimi.</summary>
public interface IDepartmentService
{
    Task<DepartmentResponseDto> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken cancellationToken = default);

    Task<DepartmentResponseDto> UpdateAsync(
        int departmentId,
        UpdateDepartmentDto dto,
        CancellationToken cancellationToken = default);

    Task<List<DepartmentResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
