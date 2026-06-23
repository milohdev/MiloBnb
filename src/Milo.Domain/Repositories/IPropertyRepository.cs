using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Property> Items, int TotalCount)> GetAllAsync(
        string? city, DateOnly? checkIn, DateOnly? checkOut, int? maxGuests,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Property>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    void Add(Property property);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
