using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Dashboard.Queries.GetOwnerDashboard;

public record GetOwnerDashboardQuery(DateOnly? DateFrom, DateOnly? DateTo)
    : IRequest<Result<OwnerDashboardDto>>;
