using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Reports.Queries.GenerateReservationsReport;

public record GenerateReservationsReportQuery(
    Guid? PropertyId,
    DateOnly? DateFrom,
    DateOnly? DateTo) : IRequest<Result<byte[]>>;
