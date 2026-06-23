using Microsoft.EntityFrameworkCore;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class PropertyRepository(MiloDbContext dbContext) : IPropertyRepository
{
    public async Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Properties
               .Include(p => p.Images)
               .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Property> Items, int TotalCount)> GetAllAsync(
        string? city, DateOnly? checkIn, DateOnly? checkOut, int? maxGuests,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Properties
            .Include(p => p.Images)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower() == city.ToLower());

        if (maxGuests.HasValue)
            query = query.Where(p => p.MaxGuests >= maxGuests.Value);

        if (checkIn.HasValue && checkOut.HasValue)
        {
            query = query.Where(p => !dbContext.Reservations.Any(r =>
                r.PropertyId == p.Id &&
                r.Status != ReservationStatus.Cancelled &&
                r.CheckInDate < checkOut.Value &&
                r.CheckOutDate > checkIn.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Property>> GetByOwnerIdAsync(
        Guid ownerId, CancellationToken cancellationToken = default)
        => await dbContext.Properties
               .Include(p => p.Images)
               .Where(p => p.OwnerId == ownerId)
               .OrderByDescending(p => p.CreatedAt)
               .ToListAsync(cancellationToken);

    public void Add(Property property) => dbContext.Properties.Add(property);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
