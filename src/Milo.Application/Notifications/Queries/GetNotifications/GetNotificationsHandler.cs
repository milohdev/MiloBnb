using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsHandler(
    INotificationRepository notificationRepository,
    ICurrentUserProvider currentUser)
    : IRequestHandler<GetNotificationsQuery, Result<Milo.Domain.Common.PagedResult<NotificationDto>>>
{
    public async Task<Result<Milo.Domain.Common.PagedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var paged = await notificationRepository.GetByUserIdAsync(
            currentUser.UserId!.Value, request.Page, request.PageSize, cancellationToken);

        return Result<Milo.Domain.Common.PagedResult<NotificationDto>>.Success(
            new Milo.Domain.Common.PagedResult<NotificationDto>
            {
                Items = paged.Items.Select(ToDto).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            });
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Title, n.Body, n.Type.ToString(), n.IsRead, n.RelatedEntityId, n.CreatedAt);
}
