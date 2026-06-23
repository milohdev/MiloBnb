using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Reservations.Queries.GetMyReservations;

namespace Milo.Application.Reservations.Commands.CreateReservation;

public record CreateReservationCommand(
    Guid PropertyId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate) : IRequest<Result<ReservationDto>>;
