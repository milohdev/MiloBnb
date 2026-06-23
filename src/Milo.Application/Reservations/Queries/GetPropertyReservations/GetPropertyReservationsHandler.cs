using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Reservations.Queries.GetMyReservations;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Reservations.Queries.GetPropertyReservations;

public sealed class GetPropertyReservationsHandler(
    IPropertyRepository propertyRepository,
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetPropertyReservationsQuery, Result<IReadOnlyList<ReservationDto>>>
{
    public async Task<Result<IReadOnlyList<ReservationDto>>> Handle(
        GetPropertyReservationsQuery request, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<IReadOnlyList<ReservationDto>>.Failure("Inmueble no encontrado");

        if (property.OwnerId != currentUser.UserId)
            return Result<IReadOnlyList<ReservationDto>>.Failure(
                "No tienes permiso para ver las reservas de este inmueble");

        var reservations = await reservationRepository.GetByPropertyIdAsync(
            request.PropertyId, cancellationToken);

        return Result<IReadOnlyList<ReservationDto>>.Success(
            reservations.Select(ToDto).ToList());
    }

    private static ReservationDto ToDto(Reservation r) =>
        new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
            $"{r.Guest.FirstName} {r.Guest.LastName}",
            r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
            r.TotalNights, r.TotalPrice, r.Status.ToString());
}
