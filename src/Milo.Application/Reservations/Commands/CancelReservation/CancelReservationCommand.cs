using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Reservations.Commands.CancelReservation;

public record CancelReservationCommand(Guid ReservationId) : IRequest<Result<bool>>;
