using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Queries.GetPropertyById;

public sealed class GetPropertyByIdHandler(
    IPropertyRepository repository) : IRequestHandler<GetPropertyByIdQuery, Result<PropertyDto>>
{
    public async Task<Result<PropertyDto>> Handle(
        GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<PropertyDto>.Failure("Inmueble no encontrado");

        return Result<PropertyDto>.Success(ToDto(property));
    }

    private static PropertyDto ToDto(Property p) =>
        new(p.Id, p.Name, p.Description, p.Address, p.City, p.Country,
            p.PricePerNight, p.MaxGuests, p.Bedrooms, p.Bathrooms,
            p.AllowSameDayBooking, p.IsActive, p.OwnerId,
            p.Images.Select(i => i.Url).ToList(),
            p.CreatedAt);
}
