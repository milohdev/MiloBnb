namespace Milo.Application.Reservations.Queries.GetMyReservations;

public record ReservationDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid GuestId,
    string GuestFullName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    DateTime CheckInDateTime,
    DateTime CheckOutDateTime,
    int TotalNights,
    decimal TotalPrice,
    string Status);
