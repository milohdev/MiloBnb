using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Reports.Queries.GenerateReservationsReport;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Owner")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("reservations")]
    public async Task<IActionResult> DownloadReservations(
        [FromQuery] Guid? propertyId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GenerateReservationsReportQuery(propertyId, dateFrom, dateTo), ct);

        if (!result.IsSuccess)
            return Problem(title: result.Error!, statusCode: StatusCodes.Status404NotFound);

        var fileName = $"reporte-reservaciones-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
