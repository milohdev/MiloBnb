using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;

namespace Milo.Domain.Entities;

public sealed class WishlistItem : BaseEntity, IAuditable
{
    private WishlistItem() { }

    public Guid GuestId { get; private set; }
    public Guid PropertyId { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Property Property { get; private set; } = null!;

    public static WishlistItem Create(Guid guestId, Guid propertyId) =>
        new() { Id = Guid.NewGuid(), GuestId = guestId, PropertyId = propertyId };
}
