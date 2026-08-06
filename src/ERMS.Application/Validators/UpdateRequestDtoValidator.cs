using ERMS.Application.DTOs.Requests;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class UpdateRequestDtoValidator : AbstractValidator<UpdateRequestDto>
{
    public UpdateRequestDtoValidator()
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
