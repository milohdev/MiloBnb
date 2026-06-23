using FluentValidation;
using Milo.Domain.Entities.Enums;

namespace Milo.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.Role)
            .Must(r => Enum.TryParse<UserRole>(r, ignoreCase: true, out var parsed)
                       && parsed != UserRole.Admin)
            .WithMessage("El rol debe ser 'Guest' u 'Owner'");
    }
}
