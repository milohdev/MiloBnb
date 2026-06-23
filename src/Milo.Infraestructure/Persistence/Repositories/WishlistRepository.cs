using Microsoft.EntityFrameworkCore;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class WishlistRepository(MiloDbContext dbContext) : IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistItem>> GetByGuestIdAsync(
        Guid guestId, CancellationToken cancellationToken = default)
        => await dbContext.WishlistItems
               .Include(w => w.Property)
                   .ThenInclude(p => p.Images)
               .Where(w => w.GuestId == guestId && w.Property.IsActive)
               .OrderByDescending(w => w.CreatedAt)
               .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Guid guestId, Guid propertyId, CancellationToken cancellationToken = default)
        => await dbContext.WishlistItems.AnyAsync(
               w => w.GuestId == guestId && w.PropertyId == propertyId, cancellationToken);

    public void Add(WishlistItem item)
        => dbContext.WishlistItems.Add(item);

    public async Task DeleteAsync(
        Guid guestId, Guid propertyId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishlistItems
            .FirstOrDefaultAsync(
                w => w.GuestId == guestId && w.PropertyId == propertyId, cancellationToken);

        if (item is not null)
        {
            dbContext.WishlistItems.Remove(item);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
