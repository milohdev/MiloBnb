using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Reservations.Queries.GetMyReservations;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Reservations.Commands.CreateReservation;

public sealed class CreateReservationHandler(
    IPropertyRepository propertyRepository,
    IUserRepository userRepository,
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    public async Task<Result<ReservationDto>> Handle(
        CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (user is null)
            return Result<ReservationDto>.Failure("Usuario no encontrado");

        if (!user.IsKycVerified)
            return Result<ReservationDto>.Failure(
                "Debes completar la verificación de identidad antes de reservar");

        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null || !property.IsActive)
            return Result<ReservationDto>.Failure("Inmueble no disponible");

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var minCheckIn = property.AllowSameDayBooking ? today : today.AddDays(1);
        if (request.CheckInDate < minCheckIn)
        {
            var msg = property.AllowSameDayBooking
                ? "La fecha de check-in no puede ser en el pasado"
                : "Este inmueble no permite reservas para el mismo día; la fecha mínima de check-in es mañana";
            return Result<ReservationDto>.Failure(msg);
        }

        var reservation = Reservation.Create(
            request.PropertyId, currentUser.UserId!.Value,
            request.CheckInDate, request.CheckOutDate,
            property.PricePerNight);

        var created = await reservationRepository.TryCreateSerializableAsync(reservation, cancellationToken);
        if (!created)
            return Result<ReservationDto>.Failure("Las fechas seleccionadas no están disponibles");

        var full = await reservationRepository.GetByIdAsync(reservation.Id, cancellationToken);
        return Result<ReservationDto>.Success(ToDto(full!));
    }

    private static ReservationDto ToDto(Reservation r) =>
        new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
            $"{r.Guest.FirstName} {r.Guest.LastName}",
            r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
            r.TotalNights, r.TotalPrice, r.Status.ToString());
}
