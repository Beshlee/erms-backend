using ERMS.Application.DTOs.Admin;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        // FR-02: parola hash'lenerek saklanır (PasswordHasher) — burada sadece giriş uzunluğu kontrolü.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır.");

        RuleFor(x => x.Role).IsInEnum();

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Departman seçilmelidir.");
    }
}
