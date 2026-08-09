using ERMS.Application.DTOs.Admin;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class UpdateDepartmentDtoValidator : AbstractValidator<UpdateDepartmentDto>
{
    public UpdateDepartmentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(150);
    }
}
