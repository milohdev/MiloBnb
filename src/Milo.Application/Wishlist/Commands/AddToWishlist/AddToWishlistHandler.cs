using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Wishlist.Commands.AddToWishlist;

public sealed class AddToWishlistHandler(
    IPropertyRepository propertyRepository,
    IWishlistRepository wishlistRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<AddToWishlistCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null || !property.IsActive)
            return Result<bool>.Failure("Inmueble no disponible");

        var guestId = currentUser.UserId!.Value;

        if (await wishlistRepository.ExistsAsync(guestId, request.PropertyId, cancellationToken))
            return Result<bool>.Success(true);

        var item = WishlistItem.Create(guestId, request.PropertyId);
        wishlistRepository.Add(item);
        await wishlistRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
