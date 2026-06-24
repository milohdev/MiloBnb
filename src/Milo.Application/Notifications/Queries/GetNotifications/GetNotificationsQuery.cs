using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<Milo.Domain.Common.PagedResult<NotificationDto>>>;
