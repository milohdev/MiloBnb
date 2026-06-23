using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface IPropertyImageRepository
{
    Task<PropertyImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    void Add(PropertyImage image);
    void Remove(PropertyImage image);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
