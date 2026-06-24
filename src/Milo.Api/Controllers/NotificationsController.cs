using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Notifications.Commands.MarkNotificationRead;
using Milo.Application.Notifications.Queries.GetNotifications;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetNotificationsQuery(page, pageSize), ct);
        return Ok(result.Value);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var result = await sender.Send(new GetUnreadNotificationsQuery(), ct);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess
            ? NoContent()
            : MapError(result.Error!);
    }

    private IActionResult MapError(string error) => error switch
    {
        var e when e.Contains("permiso") =>
            Problem(title: e, statusCode: StatusCodes.Status403Forbidden),
        _ => Problem(title: error, statusCode: StatusCodes.Status404NotFound)
    };
}
