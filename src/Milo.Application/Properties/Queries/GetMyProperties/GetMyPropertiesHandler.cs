using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Queries.GetMyProperties;

public sealed class GetMyPropertiesHandler(
    IPropertyRepository repository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetMyPropertiesQuery, Result<IReadOnlyList<PropertyDto>>>
{
    public async Task<Result<IReadOnlyList<PropertyDto>>> Handle(
        GetMyPropertiesQuery request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.UserId!.Value;
        var properties = await repository.GetByOwnerIdAsync(ownerId, cancellationToken);
        return Result<IReadOnlyList<PropertyDto>>.Success(properties.Select(ToDto).ToList());
    }

    private static PropertyDto ToDto(Property p) =>
        new(p.Id, p.Name, p.Description, p.Address, p.City, p.Country,
            p.PricePerNight, p.MaxGuests, p.Bedrooms, p.Bathrooms,
            p.AllowSameDayBooking, p.IsActive, p.OwnerId,
            p.Images.Select(i => i.Url).ToList(),
            p.CreatedAt);
}
