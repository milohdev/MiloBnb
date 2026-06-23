using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByGuestIdAsync(Guid guestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingReservationAsync(
        Guid propertyId, DateOnly checkIn, DateOnly checkOut,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Crea la reserva dentro de una transacción Serializable para prevenir double-booking.
    /// Retorna false si ya existe una reserva solapada (sin lanzar excepción).
    /// </summary>
    Task<bool> TryCreateSerializableAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
