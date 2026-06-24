using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Dashboard.Queries.GetOwnerDashboard;
using Milo.Application.Dashboard.Queries.GetPropertyMetrics;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Owner")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await sender.Send(new GetOwnerDashboardQuery(dateFrom, dateTo), ct);
        return Ok(result.Value);
    }

    [HttpGet("properties/{propertyId:guid}")]
    public async Task<IActionResult> GetPropertyMetrics(
        Guid propertyId,
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetPropertyMetricsQuery(propertyId, dateFrom, dateTo), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error!, statusCode: StatusCodes.Status404NotFound);
    }
}
