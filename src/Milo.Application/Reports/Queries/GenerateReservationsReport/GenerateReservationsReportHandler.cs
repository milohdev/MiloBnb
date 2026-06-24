using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Reports.Queries.GenerateReservationsReport;

public sealed class GenerateReservationsReportHandler(
    IPropertyRepository propertyRepository,
    IReservationRepository reservationRepository,
    IExcelReportService excelReportService,
    ICurrentUserProvider currentUser) : IRequestHandler<GenerateReservationsReportQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(
        GenerateReservationsReportQuery request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.UserId!.Value;

        if (request.PropertyId.HasValue)
        {
            var property = await propertyRepository.GetByIdAsync(request.PropertyId.Value, cancellationToken);
            if (property is null || property.OwnerId != ownerId)
                return Result<byte[]>.Failure("Inmueble no encontrado");
        }

        var reservations = await reservationRepository.GetConfirmedForReportAsync(
            ownerId, request.PropertyId, request.DateFrom, request.DateTo, cancellationToken);

        var rows = reservations.Select(r => new ReservationReportRow(
            r.Property.Name, r.Property.City,
            r.CheckInDate, r.CheckOutDate,
            r.TotalNights, r.TotalPrice,
            r.Guest.FirstName, r.Guest.LastName, r.Guest.Email));

        var bytes = await excelReportService.GenerateReservationsReportAsync(
            rows, "Reporte de Reservaciones");

        return Result<byte[]>.Success(bytes);
    }
}
