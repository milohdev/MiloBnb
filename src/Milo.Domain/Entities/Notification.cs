using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;
using Milo.Domain.Entities.Enums;

namespace Milo.Domain.Entities;

public sealed class Notification : BaseEntity, IAuditable
{
    private Notification() { }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static Notification Create(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            IsRead = false,
            RelatedEntityId = relatedEntityId
        };

    public void MarkAsRead() => IsRead = true;
}
