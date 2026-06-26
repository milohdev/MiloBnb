using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;

namespace Milo.Application.Properties.Queries.GetMyProperties;

public record GetMyPropertiesQuery : IRequest<Result<IReadOnlyList<PropertyDto>>>;
