using Microsoft.EntityFrameworkCore;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class ReservationRepository(MiloDbContext dbContext) : IReservationRepository
{
    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetByGuestIdAsync(
        Guid guestId, CancellationToken cancellationToken = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .Where(r => r.GuestId == guestId)
               .OrderByDescending(r => r.CheckInDate)
               .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetByPropertyIdAsync(
        Guid propertyId, CancellationToken cancellationToken = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .Where(r => r.PropertyId == propertyId)
               .OrderByDescending(r => r.CheckInDate)
               .ToListAsync(cancellationToken);

    public async Task<bool> HasOverlappingReservationAsync(
        Guid propertyId, DateOnly checkIn, DateOnly checkOut,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
        => await dbContext.Reservations.AnyAsync(r =>
               r.PropertyId == propertyId &&
               r.Status != ReservationStatus.Cancelled &&
               (excludeReservationId == null || r.Id != excludeReservationId) &&
               r.CheckInDate < checkOut &&
               r.CheckOutDate > checkIn, cancellationToken);

    public async Task<bool> TryCreateSerializableAsync(
        Reservation reservation, CancellationToken cancellationToken = default)
    {
        await using var tx = await dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var hasOverlap = await dbContext.Reservations.AnyAsync(r =>
            r.PropertyId == reservation.PropertyId &&
            r.Status != ReservationStatus.Cancelled &&
            r.CheckInDate < reservation.CheckOutDate &&
            r.CheckOutDate > reservation.CheckInDate, cancellationToken);

        if (hasOverlap)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Reservation>> GetConfirmedByOwnerAsync(
        Guid ownerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await dbContext.Reservations
               .Where(r => r.Property.OwnerId == ownerId
                        && r.Status == ReservationStatus.Confirmed
                        && r.CheckInDate >= from
                        && r.CheckInDate <= to)
               .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetConfirmedByPropertyAsync(
        Guid propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await dbContext.Reservations
               .Where(r => r.PropertyId == propertyId
                        && r.Status == ReservationStatus.Confirmed
                        && r.CheckInDate >= from
                        && r.CheckInDate <= to)
               .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetConfirmedForReportAsync(
        Guid ownerId, Guid? propertyId, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Reservations
            .Include(r => r.Property)
            .Include(r => r.Guest)
            .Where(r => r.Property.OwnerId == ownerId
                     && r.Status == ReservationStatus.Confirmed);

        if (propertyId.HasValue)
            query = query.Where(r => r.PropertyId == propertyId.Value);
        if (from.HasValue)
            query = query.Where(r => r.CheckInDate >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.CheckInDate <= to.Value);

        return await query.OrderBy(r => r.CheckInDate).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
