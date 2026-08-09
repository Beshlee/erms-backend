using ERMS.Application.DTOs.Admin;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class UpdateRequestTypeDtoValidator : AbstractValidator<UpdateRequestTypeDto>
{
    public UpdateRequestTypeDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Talep türü adı zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
