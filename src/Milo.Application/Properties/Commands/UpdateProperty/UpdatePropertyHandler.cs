using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Commands.UpdateProperty;

public sealed class UpdatePropertyHandler(
    IPropertyRepository repository,
    ICurrentUserProvider currentUser) : IRequestHandler<UpdatePropertyCommand, Result<PropertyDto>>
{
    public async Task<Result<PropertyDto>> Handle(
        UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<PropertyDto>.Failure("Inmueble no encontrado");

        if (property.OwnerId != currentUser.UserId)
            return Result<PropertyDto>.Failure("No tienes permiso para modificar este inmueble");

        property.Update(
            request.Name, request.Description, request.Address, request.City, request.Country,
            request.PricePerNight, request.MaxGuests, request.Bedrooms, request.Bathrooms,
            request.AllowSameDayBooking);

        await repository.SaveChangesAsync(cancellationToken);

        return Result<PropertyDto>.Success(ToDto(property));
    }

    private static PropertyDto ToDto(Property p) =>
        new(p.Id, p.Name, p.Description, p.Address, p.City, p.Country,
            p.PricePerNight, p.MaxGuests, p.Bedrooms, p.Bathrooms,
            p.AllowSameDayBooking, p.IsActive, p.OwnerId,
            p.Images.Select(i => i.Url).ToList(),
            p.CreatedAt);
}
