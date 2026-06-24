using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Reservations.Queries.GetMyReservations;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Dashboard.Queries.GetPropertyMetrics;

public sealed class GetPropertyMetricsHandler(
    IPropertyRepository propertyRepository,
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetPropertyMetricsQuery, Result<PropertyMetricsDto>>
{
    public async Task<Result<PropertyMetricsDto>> Handle(
        GetPropertyMetricsQuery request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.UserId!.Value;

        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null || property.OwnerId != ownerId)
            return Result<PropertyMetricsDto>.Failure("Inmueble no encontrado");

        var (from, to) = ResolvePeriod(request.DateFrom, request.DateTo);

        var reservations = await reservationRepository.GetConfirmedByPropertyAsync(
            request.PropertyId, from, to, cancellationToken);

        var allReservations = await reservationRepository.GetByPropertyIdAsync(
            request.PropertyId, cancellationToken);

        var totalReservations = reservations.Count;
        var totalRevenue = reservations.Sum(r => r.TotalPrice);
        var totalDays = to.DayNumber - from.DayNumber;
        var occupiedDays = reservations.Sum(r => r.TotalNights);
        var occupancyRate = totalDays > 0
            ? (double)occupiedDays / (double)totalDays * 100
            : 0d;
        var averageLengthOfStay = totalReservations > 0
            ? reservations.Average(r => (double)r.TotalNights)
            : 0d;
        var recentReservations = allReservations.Take(5).Select(ToReservationDto).ToList();

        return Result<PropertyMetricsDto>.Success(new PropertyMetricsDto(
            property.Id, property.Name, property.PricePerNight, property.AllowSameDayBooking,
            totalReservations, totalRevenue, occupancyRate, averageLengthOfStay,
            from, to, recentReservations));
    }

    private static ReservationDto ToReservationDto(Reservation r) =>
        new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
            $"{r.Guest.FirstName} {r.Guest.LastName}",
            r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
            r.TotalNights, r.TotalPrice, r.Status.ToString());

    private static (DateOnly From, DateOnly To) ResolvePeriod(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return (dateFrom, dateTo) switch
        {
            (null, null) => (today.AddDays(-30), today),
            ({ } from, null) => (from, today),
            (null, { } to) => (new DateOnly(2020, 1, 1), to),
            ({ } from, { } to) => (from, to)
        };
    }
}
