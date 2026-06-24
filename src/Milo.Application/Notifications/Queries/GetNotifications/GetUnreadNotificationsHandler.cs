using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Notifications.Queries.GetNotifications;

public sealed class GetUnreadNotificationsHandler(
    INotificationRepository notificationRepository,
    ICurrentUserProvider currentUser)
    : IRequestHandler<GetUnreadNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var items = await notificationRepository.GetUnreadByUserIdAsync(
            currentUser.UserId!.Value, cancellationToken);

        return Result<IReadOnlyList<NotificationDto>>.Success(
            items.Select(ToDto).ToList());
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Title, n.Body, n.Type.ToString(), n.IsRead, n.RelatedEntityId, n.CreatedAt);
}
