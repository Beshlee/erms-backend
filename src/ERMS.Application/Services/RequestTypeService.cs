using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using FluentValidation;

namespace ERMS.Application.Services;

/// <summary>Admin talep türü yönetimi (FR-13..15) — İzin/Masraf/vb. türlerin CRUD'u.</summary>
public sealed class RequestTypeService : IRequestTypeService
{
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRequestTypeDto> _createValidator;
    private readonly IValidator<UpdateRequestTypeDto> _updateValidator;

    public RequestTypeService(
        IRepository<RequestType> requestTypeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateRequestTypeDto> createValidator,
        IValidator<UpdateRequestTypeDto> updateValidator)
    {
        _requestTypeRepository = requestTypeRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<RequestTypeResponseDto> CreateAsync(
        CreateRequestTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var existing = await _requestTypeRepository.FirstOrDefaultAsync(x => x.Name == dto.Name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictAppException("Bu isimde bir talep türü zaten var.");
        }

        var requestType = new RequestType
        {
            Name = dto.Name,
            Description = dto.Description,
            RequiresApproval = dto.RequiresApproval,
            IsActive = true
        };

        await _requestTypeRepository.AddAsync(requestType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(requestType);
    }

    public async Task<RequestTypeResponseDto> UpdateAsync(
        int requestTypeId,
        UpdateRequestTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, dto, cancellationToken);

        var requestType = await _requestTypeRepository.GetByIdAsync(requestTypeId, cancellationToken)
            ?? throw new NotFoundAppException("Talep türü bulunamadı.");

        requestType.Name = dto.Name;
        requestType.Description = dto.Description;
        requestType.RequiresApproval = dto.RequiresApproval;
        // FR-12/FR-15: soft-delete — pasife alınan tür yeni talep oluştururken seçilemez
        // (bkz. RequestService.GetActiveRequestTypeAsync).
        requestType.IsActive = dto.IsActive;

        _requestTypeRepository.Update(requestType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(requestType);
    }

    public async Task<List<RequestTypeResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var types = await _requestTypeRepository.GetAllAsync(cancellationToken);

        return types.Select(Map).ToList();
    }

    public async Task<List<RequestTypeResponseDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        // FR-15: pasif türler, talep oluşturma formunda seçenek olarak görünmemeli.
        var types = await _requestTypeRepository.FindAsync(x => x.IsActive, cancellationToken);

        return types.Select(Map).ToList();
    }

    private static RequestTypeResponseDto Map(RequestType requestType)
    {
        return new RequestTypeResponseDto(
            requestType.Id,
            requestType.Name,
            requestType.Description,
            requestType.RequiresApproval,
            requestType.IsActive);
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
