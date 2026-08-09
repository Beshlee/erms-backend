using ERMS.Application.DTOs.Admin;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class CreateDepartmentDtoValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(150);
    }
}
