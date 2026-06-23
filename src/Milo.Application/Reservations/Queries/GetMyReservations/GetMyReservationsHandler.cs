using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Reservations.Queries.GetMyReservations;

public sealed class GetMyReservationsHandler(
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetMyReservationsQuery, Result<IReadOnlyList<ReservationDto>>>
{
    public async Task<Result<IReadOnlyList<ReservationDto>>> Handle(
        GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var reservations = await reservationRepository.GetByGuestIdAsync(
            currentUser.UserId!.Value, cancellationToken);

        return Result<IReadOnlyList<ReservationDto>>.Success(
            reservations.Select(ToDto).ToList());
    }

    private static ReservationDto ToDto(Reservation r) =>
        new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
            $"{r.Guest.FirstName} {r.Guest.LastName}",
            r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
            r.TotalNights, r.TotalPrice, r.Status.ToString());
}
