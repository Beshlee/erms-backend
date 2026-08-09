using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using FluentValidation;

namespace ERMS.Application.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IRepository<Department> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDepartmentDto> _createValidator;
    private readonly IValidator<UpdateDepartmentDto> _updateValidator;

    public DepartmentService(
        IRepository<Department> departmentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateDepartmentDto> createValidator,
        IValidator<UpdateDepartmentDto> updateValidator)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<DepartmentResponseDto> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var existing = await _departmentRepository.FirstOrDefaultAsync(d => d.Name == dto.Name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictAppException("Bu isimde bir departman zaten var.");
        }

        var department = new Department
        {
            Name = dto.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepartmentResponseDto(department.Id, department.Name, department.IsActive, department.CreatedAt);
    }

    public async Task<DepartmentResponseDto> UpdateAsync(
        int departmentId,
        UpdateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, dto, cancellationToken);

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new NotFoundAppException("Departman bulunamadı.");

        department.Name = dto.Name;
        // FR-12: soft-delete — IsActive=false.
        department.IsActive = dto.IsActive;

        _departmentRepository.Update(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepartmentResponseDto(department.Id, department.Name, department.IsActive, department.CreatedAt);
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);

        return departments
            .Select(d => new DepartmentResponseDto(d.Id, d.Name, d.IsActive, d.CreatedAt))
            .ToList();
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationAppException(errors);
        }
    }
}
