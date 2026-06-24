using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;

namespace Milo.Application.Reservations.Commands.CancelReservation;

public sealed class CancelReservationHandler(
    IReservationRepository reservationRepository,
    INotificationService notificationService,
    ICurrentUserProvider currentUser) : IRequestHandler<CancelReservationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
            return Result<bool>.Failure("Reserva no encontrada");

        if (reservation.GuestId != currentUser.UserId)
            return Result<bool>.Failure("No tienes permiso para cancelar esta reserva");

        if (reservation.Status == ReservationStatus.Cancelled)
            return Result<bool>.Failure("La reserva ya está cancelada");

        reservation.Cancel();
        await reservationRepository.SaveChangesAsync(cancellationToken);

        await notificationService.SendAsync(
            reservation.GuestId,
            "Reserva cancelada",
            $"Tu reserva en {reservation.Property.Name} del {reservation.CheckInDate:dd/MM/yyyy} al {reservation.CheckOutDate:dd/MM/yyyy} fue cancelada.",
            NotificationType.ReservationCancelled,
            relatedEntityId: reservation.Id,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
