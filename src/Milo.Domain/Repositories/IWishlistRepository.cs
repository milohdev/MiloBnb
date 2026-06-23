using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface IWishlistRepository
{
    Task<IReadOnlyList<WishlistItem>> GetByGuestIdAsync(Guid guestId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid guestId, Guid propertyId, CancellationToken cancellationToken = default);
    void Add(WishlistItem item);
    Task DeleteAsync(Guid guestId, Guid propertyId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
