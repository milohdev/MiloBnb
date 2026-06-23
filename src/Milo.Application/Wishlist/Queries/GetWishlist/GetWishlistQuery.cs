using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;

namespace Milo.Application.Wishlist.Queries.GetWishlist;

public record GetWishlistQuery : IRequest<Result<IReadOnlyList<PropertyDto>>>;
