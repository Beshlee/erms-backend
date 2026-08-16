using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Admin;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using FluentValidation;

namespace ERMS.Application.Services;

/// <summary>Admin kullanıcı yönetimi (FR-07..10) — kullanıcı oluşturma/güncelleme/listeleme.</summary>
public sealed class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;

    public UserService(
        IRepository<User> userRepository,
        IRepository<Department> departmentRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UserResponseDto> CreateAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var existing = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictAppException("Bu e-posta adresi zaten kullanılıyor.");
        }

        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId, cancellationToken)
            ?? throw new NotFoundAppException("Departman bulunamadı.");

        if (dto.ManagerId is not null)
        {
            var manager = await _userRepository.GetByIdAsync(dto.ManagerId.Value, cancellationToken);
            if (manager is null)
            {
                throw new NotFoundAppException("Belirtilen yönetici bulunamadı.");
            }

            // FR-32/US-06: onay akışı yalnızca Manager rolündeki kullanıcılar üzerinden işler
            // ([Authorize(Roles = Manager)] — ApprovalsController). Manager olmayan biri
            // ManagerId olarak atanırsa, o kişiye bağlı personelin talepleri hiçbir zaman
            // onay ekranına düşmez (sessiz bir çıkmaz) — bu yüzden burada engelliyoruz.
            if (manager.Role != UserRole.Manager)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["managerId"] = ["Belirtilen kullanıcı Manager rolünde değil."]
                });
            }
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            // FR-02: parola asla düz metin saklanmaz.
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = dto.Role,
            DepartmentId = department.Id,
            ManagerId = dto.ManagerId,
            IsActive = true
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), department.Id, department.Name, user.ManagerId, user.IsActive);
    }

    public async Task<UserResponseDto> UpdateAsync(
        int userId,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, dto, cancellationToken);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundAppException("Kullanıcı bulunamadı.");

        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId, cancellationToken)
            ?? throw new NotFoundAppException("Departman bulunamadı.");

        if (dto.ManagerId is not null)
        {
            if (dto.ManagerId == userId)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["managerId"] = ["Bir kullanıcı kendi yöneticisi olamaz."]
                });
            }

            var manager = await _userRepository.GetByIdAsync(dto.ManagerId.Value, cancellationToken);
            if (manager is null)
            {
                throw new NotFoundAppException("Belirtilen yönetici bulunamadı.");
            }

            // FR-32/US-06 ile aynı gerekçe (bkz. CreateAsync) — Manager olmayan biri
            // yönetici olarak atanamaz.
            if (manager.Role != UserRole.Manager)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["managerId"] = ["Belirtilen kullanıcı Manager rolünde değil."]
                });
            }
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Role = dto.Role;
        user.DepartmentId = department.Id;
        user.ManagerId = dto.ManagerId;
        // FR-12: fiziksel silme yerine soft-delete — IsActive=false ile pasife alınır.
        // FR-08: pasif kullanıcı zaten AuthService.LoginAsync'te reddediliyor.
        user.IsActive = dto.IsActive;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), department.Id, department.Name, user.ManagerId, user.IsActive);
    }

    public async Task<List<UserResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        var departmentNames = departments.ToDictionary(d => d.Id, d => d.Name);

        return users
            .Select(u => new UserResponseDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role.ToString(),
                u.DepartmentId,
                departmentNames.TryGetValue(u.DepartmentId, out var name) ? name : "Bilinmiyor",
                u.ManagerId,
                u.IsActive))
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
