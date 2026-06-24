namespace Milo.Application.Notifications.Queries.GetNotifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    bool IsRead,
    Guid? RelatedEntityId,
    DateTime CreatedAt);
