using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Wishlist.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid PropertyId) : IRequest<Result<bool>>;
