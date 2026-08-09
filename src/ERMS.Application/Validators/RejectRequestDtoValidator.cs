using ERMS.Application.DTOs.Approvals;
using FluentValidation;

namespace ERMS.Application.Validators;

/// <summary>FR-34 — reddetme işleminde gerekçe/açıklama girmek zorunludur.</summary>
public sealed class RejectRequestDtoValidator : AbstractValidator<RejectRequestDto>
{
    public RejectRequestDtoValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Reddetme gerekçesi zorunludur.")
            .MaximumLength(1000).WithMessage("Gerekçe en fazla 1000 karakter olabilir.");
    }
}
