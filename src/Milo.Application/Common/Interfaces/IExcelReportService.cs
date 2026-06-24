namespace Milo.Application.Common.Interfaces;

public interface IExcelReportService
{
    Task<byte[]> GenerateReservationsReportAsync(
        IEnumerable<ReservationReportRow> rows, string reportTitle);
}

public record ReservationReportRow(
    string PropertyName,
    string PropertyCity,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int TotalNights,
    decimal TotalPrice,
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail);
