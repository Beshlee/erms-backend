using ERMS.Application.DTOs.Requests;
using FluentValidation;

namespace ERMS.Application.Validators;

public sealed class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(2000).WithMessage("Yorum en fazla 2000 karakter olabilir.");
    }
}
