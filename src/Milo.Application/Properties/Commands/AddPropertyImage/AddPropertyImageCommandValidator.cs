using FluentValidation;

namespace Milo.Application.Properties.Commands.AddPropertyImage;

public sealed class AddPropertyImageCommandValidator : AbstractValidator<AddPropertyImageCommand>
{
    public AddPropertyImageCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("La URL no es válida");
    }
}
