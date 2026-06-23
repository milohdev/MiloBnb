using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;

namespace Milo.Application.Properties.Queries.GetPropertyById;

public record GetPropertyByIdQuery(Guid PropertyId) : IRequest<Result<PropertyDto>>;
