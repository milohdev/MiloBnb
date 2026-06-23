using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Properties.Queries.GetProperties;

public record GetPropertiesQuery(
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? MaxGuests,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<PropertyDto>>>;
