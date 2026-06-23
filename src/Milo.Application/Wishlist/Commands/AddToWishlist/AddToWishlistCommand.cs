using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Wishlist.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid PropertyId) : IRequest<Result<bool>>;
