using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Reservations.Queries.GetMyReservations;

public record GetMyReservationsQuery : IRequest<Result<IReadOnlyList<ReservationDto>>>;
