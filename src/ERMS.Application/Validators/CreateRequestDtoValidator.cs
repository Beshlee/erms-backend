using ERMS.Application.DTOs.Requests;
using FluentValidation;

namespace ERMS.Application.Validators;

/// <summary>
/// Yapısal doğrulama (FR-17, FR-19'un pozitiflik kısmı). Talep türüne özgü çapraz
/// kurallar (FR-18 İzin tarihleri, FR-20 tarih sırası, FR-19 Masraf tutarı zorunluluğu)
/// RequestType'ı veritabanından bilmesi gerektiği için RequestService içinde uygulanır.
/// </summary>
public sealed class CreateRequestDtoValidator : AbstractValidator<CreateRequestDto>
{
    public CreateRequestDtoValidator()
    {
        RuleFor(x => x.RequestTypeId)
            .GreaterThan(0)
            .WithMessage("Talep türü seçilmelidir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Açıklama zorunludur.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Tutar pozitif bir değer olmalıdır.")
            .When(x => x.Amount is not null);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Geçersiz öncelik değeri.");
    }
}
