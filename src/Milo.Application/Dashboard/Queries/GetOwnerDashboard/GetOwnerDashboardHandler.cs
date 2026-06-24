using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Dashboard.Queries.GetOwnerDashboard;

public sealed class GetOwnerDashboardHandler(
    IPropertyRepository propertyRepository,
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetOwnerDashboardQuery, Result<OwnerDashboardDto>>
{
    public async Task<Result<OwnerDashboardDto>> Handle(
        GetOwnerDashboardQuery request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.UserId!.Value;
        var (from, to) = ResolvePeriod(request.DateFrom, request.DateTo);

        var allProperties = await propertyRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
        var activeProperties = allProperties.Where(p => p.IsActive).ToList();

        var reservations = await reservationRepository.GetConfirmedByOwnerAsync(
            ownerId, from, to, cancellationToken);

        var totalProperties = activeProperties.Count;
        var totalReservations = reservations.Count;
        var totalRevenue = reservations.Sum(r => r.TotalPrice);
        var totalDays = to.DayNumber - from.DayNumber;
        var occupiedDays = reservations.Sum(r => r.TotalNights);
        var occupancyRate = totalDays > 0 && totalProperties > 0
            ? (double)occupiedDays / ((double)totalDays * totalProperties) * 100
            : 0d;
        var averagePricePerNight = totalProperties > 0
            ? activeProperties.Average(p => p.PricePerNight)
            : 0m;
        var reservationsByProperty = activeProperties
            .Select(p => new PropertySummaryDto(
                p.Id, p.Name,
                reservations.Count(r => r.PropertyId == p.Id),
                reservations.Where(r => r.PropertyId == p.Id).Sum(r => r.TotalPrice)))
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return Result<OwnerDashboardDto>.Success(new OwnerDashboardDto(
            totalProperties, totalReservations, totalRevenue,
            occupancyRate, averagePricePerNight,
            from, to, reservationsByProperty));
    }

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
