namespace Milo.Application.Dashboard.Queries.GetOwnerDashboard;

public record OwnerDashboardDto(
    int TotalProperties,
    int TotalReservations,
    decimal TotalRevenue,
    double OccupancyRate,
    decimal AveragePricePerNight,
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlyList<PropertySummaryDto> ReservationsByProperty);
