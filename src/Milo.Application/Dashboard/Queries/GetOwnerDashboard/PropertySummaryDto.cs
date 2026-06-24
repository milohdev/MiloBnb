namespace Milo.Application.Dashboard.Queries.GetOwnerDashboard;

public record PropertySummaryDto(
    Guid PropertyId,
    string PropertyName,
    int ReservationCount,
    decimal Revenue);
