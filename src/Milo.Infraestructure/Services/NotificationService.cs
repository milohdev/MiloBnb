using Microsoft.Extensions.Logging;
using Milo.Application.Common.Interfaces;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;

namespace Milo.Infraestructure.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, title, body, type, relatedEntityId);
        notificationRepository.Add(notification);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        logger.LogInformation(
            "Email notification sent to {Email} | Title: {Title} | Type: {Type}",
            user?.Email ?? userId.ToString(), title, type);
    }
}
