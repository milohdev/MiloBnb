using Milo.Domain.Entities.Enums;

namespace Milo.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendAsync(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}
