using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Wishlist.Queries.GetWishlist;

public sealed class GetWishlistHandler(
    IWishlistRepository wishlistRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetWishlistQuery, Result<IReadOnlyList<PropertyDto>>>
{
    public async Task<Result<IReadOnlyList<PropertyDto>>> Handle(
        GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var items = await wishlistRepository.GetByGuestIdAsync(
            currentUser.UserId!.Value, cancellationToken);

        return Result<IReadOnlyList<PropertyDto>>.Success(
            items.Select(ToDto).ToList());
    }

    private static PropertyDto ToDto(WishlistItem w) =>
        new(w.Property.Id, w.Property.Name, w.Property.Description,
            w.Property.Address, w.Property.City, w.Property.Country,
            w.Property.PricePerNight, w.Property.MaxGuests,
            w.Property.Bedrooms, w.Property.Bathrooms,
            w.Property.AllowSameDayBooking, w.Property.IsActive, w.Property.OwnerId,
            w.Property.Images.Select(i => i.Url).ToList(),
            w.Property.CreatedAt);
}
