using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Commands.CreateProperty;

public sealed class CreatePropertyHandler(
    IPropertyRepository repository,
    ICurrentUserProvider currentUser) : IRequestHandler<CreatePropertyCommand, Result<PropertyDto>>
{
    public async Task<Result<PropertyDto>> Handle(
        CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = Property.Create(
            request.Name, request.Description, request.Address, request.City, request.Country,
            request.PricePerNight, request.MaxGuests, request.Bedrooms, request.Bathrooms,
            request.AllowSameDayBooking, currentUser.UserId!.Value);

        repository.Add(property);
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
