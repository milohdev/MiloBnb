using FluentValidation;

namespace Milo.Application.Kyc.Commands.VerifyKyc;

public sealed class VerifyKycCommandValidator : AbstractValidator<VerifyKycCommand>
{
    public VerifyKycCommandValidator()
    {
        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("La URL de la imagen es requerida")
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                         && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
            .WithMessage("ImageUrl debe ser una URL HTTP/HTTPS válida");
    }
}
