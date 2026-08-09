using ERMS.Application.DTOs.Approvals;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class ApproveRequestDtoValidator : AbstractValidator<ApproveRequestDto>
{
    public ApproveRequestDtoValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");
    }
}
