using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Reservations.Queries.GetMyReservations;

namespace Milo.Application.Reservations.Queries.GetPropertyReservations;

public record GetPropertyReservationsQuery(Guid PropertyId)
    : IRequest<Result<IReadOnlyList<ReservationDto>>>;
