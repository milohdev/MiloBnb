using Milo.Application.Reservations.Queries.GetMyReservations;

namespace Milo.Application.Dashboard.Queries.GetPropertyMetrics;

public record PropertyMetricsDto(
    Guid PropertyId,
    string PropertyName,
    decimal PricePerNight,
    bool AllowSameDayBooking,
    int TotalReservations,
    decimal TotalRevenue,
    double OccupancyRate,
    double AverageLengthOfStay,
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlyList<ReservationDto> RecentReservations);
