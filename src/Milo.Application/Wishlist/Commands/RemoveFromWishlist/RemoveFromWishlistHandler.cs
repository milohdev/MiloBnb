using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Wishlist.Commands.RemoveFromWishlist;

public sealed class RemoveFromWishlistHandler(
    IWishlistRepository wishlistRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<RemoveFromWishlistCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        await wishlistRepository.DeleteAsync(
            currentUser.UserId!.Value, request.PropertyId, cancellationToken);

        return Result<bool>.Success(true);
    }
}
