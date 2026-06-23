using MediatR;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Queries.GetProperties;

public sealed class GetPropertiesHandler(
    IPropertyRepository repository) : IRequestHandler<GetPropertiesQuery, Result<PagedResult<PropertyDto>>>
{
    public async Task<Result<PagedResult<PropertyDto>>> Handle(
        GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetAllAsync(
            request.City, request.CheckIn, request.CheckOut, request.MaxGuests,
            request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(ToDto).ToList();

        return Result<PagedResult<PropertyDto>>.Success(new PagedResult<PropertyDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    private static PropertyDto ToDto(Property p) =>
        new(p.Id, p.Name, p.Description, p.Address, p.City, p.Country,
            p.PricePerNight, p.MaxGuests, p.Bedrooms, p.Bathrooms,
            p.AllowSameDayBooking, p.IsActive, p.OwnerId,
            p.Images.Select(i => i.Url).ToList(),
            p.CreatedAt);
}
