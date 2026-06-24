using Milo.Domain.Common;
using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface INotificationRepository
{
    Task<PagedResult<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Notification notification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
