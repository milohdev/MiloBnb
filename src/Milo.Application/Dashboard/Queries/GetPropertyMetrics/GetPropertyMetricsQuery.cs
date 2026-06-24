using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Dashboard.Queries.GetPropertyMetrics;

public record GetPropertyMetricsQuery(Guid PropertyId, DateOnly? DateFrom, DateOnly? DateTo)
    : IRequest<Result<PropertyMetricsDto>>;
