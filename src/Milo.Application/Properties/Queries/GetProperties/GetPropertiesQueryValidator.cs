using FluentValidation;

namespace Milo.Application.Properties.Queries.GetProperties;

public sealed class GetPropertiesQueryValidator : AbstractValidator<GetPropertiesQuery>
{
    public GetPropertiesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.MaxGuests)
            .GreaterThanOrEqualTo(1)
            .When(x => x.MaxGuests.HasValue);

        When(x => x.CheckIn.HasValue || x.CheckOut.HasValue, () =>
        {
            RuleFor(x => x.CheckIn)
                .NotNull()
                .WithMessage("checkIn es requerido cuando se especifica checkOut");
            RuleFor(x => x.CheckOut)
                .NotNull()
                .WithMessage("checkOut es requerido cuando se especifica checkIn");
            RuleFor(x => x.CheckOut)
                .GreaterThan(x => x.CheckIn!.Value)
                .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
                .WithMessage("checkOut debe ser posterior a checkIn");
        });
    }
}
