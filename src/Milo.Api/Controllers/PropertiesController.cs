using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Properties.Commands.AddPropertyImage;
using Milo.Application.Properties.Commands.CreateProperty;
using Milo.Application.Properties.Commands.DeleteProperty;
using Milo.Application.Properties.Commands.DeletePropertyImage;
using Milo.Application.Properties.Commands.UpdateProperty;
using Milo.Application.Properties.Queries.GetProperties;
using Milo.Application.Properties.Queries.GetMyProperties;
using Milo.Application.Properties.Queries.GetPropertyById;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetPropertiesQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPropertyByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    [Authorize(Roles = "Owner")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyPropertiesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdatePropertyBodyDto body, CancellationToken cancellationToken)
    {
        var cmd = new UpdatePropertyCommand(id, body.Name, body.Description, body.Address,
            body.City, body.Country, body.PricePerNight, body.MaxGuests,
            body.Bedrooms, body.Bathrooms, body.AllowSameDayBooking);
        var result = await sender.Send(cmd, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePropertyCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> AddImage(
        Guid id, [FromBody] AddImageBodyDto body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddPropertyImageCommand(id, body.Url), cancellationToken);
        return result.IsSuccess
            ? Created(string.Empty, new { imageId = result.Value })
            : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeletePropertyImageCommand(id, imageId), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    private IActionResult MapError(string error)
    {
        if (error.Contains("permiso", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status403Forbidden);
        if (error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status404NotFound);
        return Problem(title: error, statusCode: StatusCodes.Status400BadRequest);
    }
}

public record UpdatePropertyBodyDto(
    string Name,
    string Description,
    string Address,
    string City,
    string Country,
    decimal PricePerNight,
    int MaxGuests,
    int Bedrooms,
    int Bathrooms,
    bool AllowSameDayBooking);

public record AddImageBodyDto(string Url);
