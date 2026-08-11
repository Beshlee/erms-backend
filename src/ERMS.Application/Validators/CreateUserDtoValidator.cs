using ERMS.Application.DTOs.Admin;
using FluentValidation;

namespace ERMS.Application.Validators;

/// <summary>
/// FluentValidation kuralları — <c>RuleFor(x => ...)</c> zinciri neredeyse İngilizce gibi
/// okunur ("x.Email boş olmasın, e-posta formatında olsun, en fazla 256 karakter olsun").
/// ÖNEMLİ: bu proje ASP.NET Core'un otomatik model-doğrulama filtresini KAPATTI
/// (Program.cs → SuppressModelStateInvalidFilter), o yüzden bu validator'lar kendiliğinden
/// çalışmaz — her Service metodu, ilgili validator'ı DI ile alıp elle çağırır (bkz.
/// UserService.CreateAsync ya da RequestService'teki private ValidateAsync yardımcı metodu).
/// Bütün validator'lar `ApiServiceRegistration.AddValidatorsFromAssemblyContaining&lt;...&gt;()`
/// ile tek satırda otomatik keşfedilip DI konteynerine kaydedilir.
/// </summary>
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
