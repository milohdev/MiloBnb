using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadHandler(
    INotificationRepository notificationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(
            request.NotificationId, cancellationToken);

        if (notification is null)
            return Result<bool>.Failure("Notificación no encontrada");

        if (notification.UserId != currentUser.UserId)
            return Result<bool>.Failure("No tienes permiso para marcar esta notificación");

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
