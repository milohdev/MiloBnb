using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Notifications.Queries.GetNotifications;

public record GetUnreadNotificationsQuery : IRequest<Result<IReadOnlyList<NotificationDto>>>;
