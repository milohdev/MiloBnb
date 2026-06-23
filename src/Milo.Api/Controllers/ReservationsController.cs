using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Reservations.Commands.CancelReservation;
using Milo.Application.Reservations.Commands.CreateReservation;
using Milo.Application.Reservations.Queries.GetMyReservations;
using Milo.Application.Reservations.Queries.GetPropertyReservations;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Guest")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess) return MapError(result.Error!);
        return CreatedAtAction(nameof(GetMy), null, result.Value);
    }

    [Authorize(Roles = "Guest")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelReservationCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    [Authorize(Roles = "Guest")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyReservationsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Owner")]
    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPropertyReservationsQuery(propertyId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    private IActionResult MapError(string error)
    {
        if (error.Contains("permiso", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("identidad", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status403Forbidden);
        if (error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("no disponible", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status404NotFound);
        if (error.Contains("fechas seleccionadas", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status409Conflict);
        return Problem(title: error, statusCode: StatusCodes.Status400BadRequest);
    }
}
