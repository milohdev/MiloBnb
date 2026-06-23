using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Wishlist.Commands.AddToWishlist;
using Milo.Application.Wishlist.Commands.RemoveFromWishlist;
using Milo.Application.Wishlist.Queries.GetWishlist;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize(Roles = "Guest")]
public sealed class WishlistController(ISender sender) : ControllerBase
{
    [HttpPost("{propertyId:guid}")]
    public async Task<IActionResult> Add(Guid propertyId, CancellationToken ct)
    {
        var result = await sender.Send(new AddToWishlistCommand(propertyId), ct);
        return result.IsSuccess
            ? Ok()
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    [HttpDelete("{propertyId:guid}")]
    public async Task<IActionResult> Remove(Guid propertyId, CancellationToken ct)
    {
        await sender.Send(new RemoveFromWishlistCommand(propertyId), ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var result = await sender.Send(new GetWishlistQuery(), ct);
        return Ok(result.Value);
    }
}
