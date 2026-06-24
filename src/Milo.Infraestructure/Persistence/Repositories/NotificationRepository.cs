using Microsoft.EntityFrameworkCore;
using Milo.Domain.Common;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class NotificationRepository(MiloDbContext dbContext) : INotificationRepository
{
    public async Task<PagedResult<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<Notification>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
               .Where(n => n.UserId == userId && !n.IsRead)
               .OrderByDescending(n => n.CreatedAt)
               .ToListAsync(cancellationToken);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public void Add(Notification notification)
        => dbContext.Notifications.Add(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
